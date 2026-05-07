using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AgroControlAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LaboratoryController : ControllerBase
    {
        private readonly string _connectionString;

        public LaboratoryController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("raw-material-pending")]
        public async Task<IActionResult> GetRawMaterialPending()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, batch_number, raw_material_id, supplier_name, quantity_kg, arrival_date, status FROM raw_material_batches WHERE status IN ('pending', 'testing')", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            BatchNumber = reader.GetString(1),
                            RawMaterialId = reader.GetInt64(2),
                            SupplierName = reader.GetString(3),
                            QuantityKg = reader.GetDecimal(4),
                            ArrivalDate = reader.GetDateTime(5),
                            Status = reader.GetString(6)
                        });
                    }
                }
            }
            return Ok(new { success = true, data = list });
        }

        [HttpGet("product-pending")]
        public async Task<IActionResult> GetProductPending()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"SELECT pb.id, pb.batch_number, p.name, pb.status 
                                          FROM production_batches pb 
                                          INNER JOIN products p ON pb.product_id = p.id 
                                          WHERE pb.status = 'quality_control'", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            BatchNumber = reader.GetString(1),
                            ProductName = reader.GetString(2),
                            Status = reader.GetString(3)
                        });
                    }
                }
            }
            return Ok(new { success = true, data = list });
        }

        [HttpPost("create-test")]
        public async Task<IActionResult> CreateTest([FromBody] CreateLabTestDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("INSERT INTO lab_tests (test_number, object_type, object_id, test_type, status, created_at) VALUES (@num, @type, @oid, @ttype, 'created', GETDATE()); SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@num", $"LT-{System.DateTime.Now.Ticks}");
                cmd.Parameters.AddWithValue("@type", dto.ObjectType);
                cmd.Parameters.AddWithValue("@oid", dto.ObjectId);
                cmd.Parameters.AddWithValue("@ttype", dto.TestType ?? "Контроль качества");

                // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                var testId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, testId, message = "Испытание создано" });
            }
        }

        [HttpGet("test/{testId}")]
        public async Task<IActionResult> GetTest(long testId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var testCmd = new SqlCommand("SELECT id, test_number, object_type, object_id, test_type, status, decision, decision_reason, tested_at, tested_by FROM lab_tests WHERE id = @id", conn);
                testCmd.Parameters.AddWithValue("@id", testId);
                using (var reader = await testCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var test = new
                        {
                            Id = reader.GetInt64(0),
                            TestNumber = reader.GetString(1),
                            ObjectType = reader.GetString(2),
                            ObjectId = reader.GetInt64(3),
                            TestType = reader.GetString(4),
                            Status = reader.GetString(5),
                            Decision = reader.IsDBNull(6) ? null : reader.GetString(6),
                            DecisionReason = reader.IsDBNull(7) ? null : reader.GetString(7),
                            TestedAt = reader.IsDBNull(8) ? (System.DateTime?)null : reader.GetDateTime(8),
                            TestedBy = reader.IsDBNull(9) ? (long?)null : reader.GetInt64(9)
                        };

                        await reader.CloseAsync();

                        var paramList = new List<object>();
                        var paramCmd = new SqlCommand("SELECT id, parameter_name, measured_value, standard_value, unit, comment FROM test_parameters WHERE test_id = @tid", conn);
                        paramCmd.Parameters.AddWithValue("@tid", testId);
                        using (var paramReader = await paramCmd.ExecuteReaderAsync())
                        {
                            while (await paramReader.ReadAsync())
                            {
                                paramList.Add(new
                                {
                                    Id = paramReader.GetInt64(0),
                                    ParameterName = paramReader.GetString(1),
                                    MeasuredValue = paramReader.GetDecimal(2),
                                    StandardValue = paramReader.IsDBNull(3) ? null : paramReader.GetString(3),
                                    Unit = paramReader.IsDBNull(4) ? null : paramReader.GetString(4),
                                    Comment = paramReader.IsDBNull(5) ? null : paramReader.GetString(5)
                                });
                            }
                        }

                        return Ok(new { success = true, data = test, parameters = paramList });
                    }
                }
            }
            return NotFound(new { success = false, message = "Испытание не найдено" });
        }

        [HttpPost("save-draft/{testId}")]
        public async Task<IActionResult> SaveDraftResults(long testId, [FromBody] List<LabTestParameterDto> parameters)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var statusCmd = new SqlCommand("UPDATE lab_tests SET status = 'in_progress' WHERE id = @id", conn, trans);
                    statusCmd.Parameters.AddWithValue("@id", testId);
                    await statusCmd.ExecuteNonQueryAsync();

                    var delCmd = new SqlCommand("DELETE FROM test_parameters WHERE test_id = @id", conn, trans);
                    delCmd.Parameters.AddWithValue("@id", testId);
                    await delCmd.ExecuteNonQueryAsync();

                    foreach (var param in parameters)
                    {
                        var insCmd = new SqlCommand(@"
                            INSERT INTO test_parameters (test_id, parameter_name, measured_value, standard_value, unit, comment)
                            VALUES (@tid, @name, @val, @std, @unit, @comment)", conn, trans);
                        insCmd.Parameters.AddWithValue("@tid", testId);
                        insCmd.Parameters.AddWithValue("@name", param.ParameterName);
                        insCmd.Parameters.AddWithValue("@val", param.MeasuredValue);
                        insCmd.Parameters.AddWithValue("@std", (object)param.StandardValue ?? DBNull.Value);
                        insCmd.Parameters.AddWithValue("@unit", (object)param.Unit ?? DBNull.Value);
                        insCmd.Parameters.AddWithValue("@comment", (object)param.Comment ?? DBNull.Value);
                        await insCmd.ExecuteNonQueryAsync();
                    }

                    trans.Commit();
                    return Ok(new { success = true, message = "Промежуточные результаты сохранены" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpPost("submit-result")]
        public async Task<IActionResult> SubmitResult([FromBody] LabTestResultDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var updateCmd = new SqlCommand("UPDATE lab_tests SET status = 'completed', decision = @dec, decision_reason = @reason, tested_at = GETDATE(), tested_by = @by WHERE id = @id", conn, trans);
                    updateCmd.Parameters.AddWithValue("@dec", dto.Decision);
                    updateCmd.Parameters.AddWithValue("@reason", (object)dto.DecisionReason ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@by", dto.TestedBy);
                    updateCmd.Parameters.AddWithValue("@id", dto.TestId);
                    await updateCmd.ExecuteNonQueryAsync();

                    var objCmd = new SqlCommand("SELECT object_type, object_id FROM lab_tests WHERE id = @id", conn, trans);
                    objCmd.Parameters.AddWithValue("@id", dto.TestId);
                    using (var reader = await objCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string objectType = reader.GetString(0);
                            long objectId = reader.GetInt64(1);
                            await reader.CloseAsync();

                            string newStatus = dto.Decision == "approved" ? "approved" : "blocked";

                            if (objectType == "product")
                            {
                                var batchCmd = new SqlCommand("UPDATE production_batches SET status = @status WHERE id = @id", conn, trans);
                                batchCmd.Parameters.AddWithValue("@status", newStatus);
                                batchCmd.Parameters.AddWithValue("@id", objectId);
                                await batchCmd.ExecuteNonQueryAsync();
                            }
                            else if (objectType == "raw_material")
                            {
                                var rawCmd = new SqlCommand("UPDATE raw_material_batches SET status = @status WHERE id = @id", conn, trans);
                                rawCmd.Parameters.AddWithValue("@status", newStatus);
                                rawCmd.Parameters.AddWithValue("@id", objectId);
                                await rawCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    trans.Commit();
                    return Ok(new { success = true, message = "Результаты испытания сохранены" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpGet("protocols")]
        public async Task<IActionResult> GetProtocols(
            [FromQuery] string objectType = null,
            [FromQuery] System.DateTime? dateFrom = null,
            [FromQuery] System.DateTime? dateTo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var list = new List<object>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var countCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM lab_tests
                    WHERE status = 'completed'
                      AND (@objType IS NULL OR object_type = @objType)
                      AND (@dateFrom IS NULL OR tested_at >= @dateFrom)
                      AND (@dateTo IS NULL OR tested_at <= @dateTo)", conn);
                countCmd.Parameters.AddWithValue("@objType", (object)objectType ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@dateFrom", (object)dateFrom ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@dateTo", (object)dateTo ?? DBNull.Value);
                totalCount = (int)await countCmd.ExecuteScalarAsync();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                    SELECT lt.id, lt.test_number, lt.object_type, lt.object_id, lt.test_type, 
                           lt.decision, lt.tested_at, lt.tested_by, u.full_name as tested_by_name,
                           (SELECT COUNT(*) FROM test_parameters WHERE test_id = lt.id) as parameters_count
                    FROM lab_tests lt
                    LEFT JOIN users u ON lt.tested_by = u.id
                    WHERE lt.status = 'completed'
                      AND (@objType IS NULL OR lt.object_type = @objType)
                      AND (@dateFrom IS NULL OR lt.tested_at >= @dateFrom)
                      AND (@dateTo IS NULL OR lt.tested_at <= @dateTo)
                    ORDER BY lt.tested_at DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@objType", (object)objectType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dateFrom", (object)dateFrom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dateTo", (object)dateTo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            TestNumber = reader.GetString(1),
                            ObjectType = reader.GetString(2),
                            ObjectId = reader.GetInt64(3),
                            TestType = reader.GetString(4),
                            Decision = reader.GetString(5),
                            TestedAt = reader.GetDateTime(6),
                            TestedBy = reader.GetInt64(7),
                            TestedByName = reader.IsDBNull(8) ? null : reader.GetString(8),
                            ParametersCount = reader.GetInt32(9)
                        });
                    }
                }
            }

            return Ok(new
            {
                success = true,
                data = list,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)System.Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        [HttpGet("protocols/{testId}")]
        public async Task<IActionResult> GetProtocolDetails(long testId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var testCmd = new SqlCommand(@"
                    SELECT lt.id, lt.test_number, lt.object_type, lt.object_id, lt.test_type, 
                           lt.decision, lt.decision_reason, lt.tested_at, lt.tested_by, u.full_name as tested_by_name,
                           CASE 
                               WHEN lt.object_type = 'product' THEN p.name
                               WHEN lt.object_type = 'raw_material' THEN rm.name
                               ELSE NULL
                           END as object_name
                    FROM lab_tests lt
                    LEFT JOIN users u ON lt.tested_by = u.id
                    LEFT JOIN products p ON lt.object_type = 'product' AND lt.object_id = p.id
                    LEFT JOIN raw_materials rm ON lt.object_type = 'raw_material' AND lt.object_id = rm.id
                    WHERE lt.id = @id AND lt.status = 'completed'", conn);
                testCmd.Parameters.AddWithValue("@id", testId);

                using (var reader = await testCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var test = new
                        {
                            Id = reader.GetInt64(0),
                            TestNumber = reader.GetString(1),
                            ObjectType = reader.GetString(2),
                            ObjectId = reader.GetInt64(3),
                            ObjectName = reader.IsDBNull(10) ? null : reader.GetString(10),
                            TestType = reader.GetString(4),
                            Decision = reader.GetString(5),
                            DecisionReason = reader.IsDBNull(6) ? null : reader.GetString(6),
                            TestedAt = reader.GetDateTime(7),
                            TestedBy = reader.GetInt64(8),
                            TestedByName = reader.IsDBNull(9) ? null : reader.GetString(9)
                        };

                        await reader.CloseAsync();

                        var paramList = new List<object>();
                        var paramCmd = new SqlCommand("SELECT parameter_name, measured_value, standard_value, unit, comment FROM test_parameters WHERE test_id = @tid", conn);
                        paramCmd.Parameters.AddWithValue("@tid", testId);
                        using (var paramReader = await paramCmd.ExecuteReaderAsync())
                        {
                            while (await paramReader.ReadAsync())
                            {
                                paramList.Add(new
                                {
                                    ParameterName = paramReader.GetString(0),
                                    MeasuredValue = paramReader.GetDecimal(1),
                                    StandardValue = paramReader.IsDBNull(2) ? null : paramReader.GetString(2),
                                    Unit = paramReader.IsDBNull(3) ? null : paramReader.GetString(3),
                                    Comment = paramReader.IsDBNull(4) ? null : paramReader.GetString(4)
                                });
                            }
                        }

                        return Ok(new { success = true, data = test, parameters = paramList });
                    }
                }
            }
            return NotFound(new { success = false, message = "Протокол не найден" });
        }

        [HttpPost("block-batch/{batchId}")]
        public async Task<IActionResult> BlockBatch(long batchId, [FromBody] string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return BadRequest(new { success = false, message = "Причина блокировки обязательна" });

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE production_batches SET status = 'blocked' WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", batchId);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Партия заблокирована" });
            }
        }

        [HttpPost("approve-batch/{batchId}")]
        public async Task<IActionResult> ApproveBatch(long batchId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE production_batches SET status = 'completed' WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", batchId);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Партия разрешена к использованию" });
            }
        }
    }
}