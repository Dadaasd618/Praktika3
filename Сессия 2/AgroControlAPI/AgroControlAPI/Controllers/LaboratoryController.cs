using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AgroControlAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
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
            return Ok(list);
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
            return Ok(list);
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
                var testId = (long)await cmd.ExecuteScalarAsync();
                return Ok(new { success = true, testId });
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
                    return Ok(new { success = true });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
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
                return Ok(new { success = true });
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
                return Ok(new { success = true });
            }
        }
    }
}