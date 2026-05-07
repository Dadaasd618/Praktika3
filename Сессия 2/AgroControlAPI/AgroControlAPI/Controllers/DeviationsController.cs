using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DeviationsController : ControllerBase
    {
        private readonly string _connectionString;

        public DeviationsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Deviations
        [HttpGet]
        public async Task<IActionResult> GetDeviations(
            [FromQuery] string status = null,
            [FromQuery] string severity = null,
            [FromQuery] long? batchId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var list = new List<object>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var countCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM deviations
                    WHERE (@status IS NULL OR status = @status)
                      AND (@severity IS NULL OR severity = @severity)
                      AND (@batchId IS NULL OR batch_id = @batchId)", conn);
                countCmd.Parameters.AddWithValue("@status", (object)status ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@severity", (object)severity ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@batchId", (object)batchId ?? DBNull.Value);
                totalCount = (int)await countCmd.ExecuteScalarAsync();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                    SELECT id, batch_id, step_name, parameter_name, planned_value, actual_value, 
                           severity, status, created_at, created_by
                    FROM deviations
                    WHERE (@status IS NULL OR status = @status)
                      AND (@severity IS NULL OR severity = @severity)
                      AND (@batchId IS NULL OR batch_id = @batchId)
                    ORDER BY created_at DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@status", (object)status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@severity", (object)severity ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@batchId", (object)batchId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            BatchId = reader.GetInt64(1),
                            StepName = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ParameterName = reader.IsDBNull(3) ? null : reader.GetString(3),
                            PlannedValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                            ActualValue = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Severity = reader.GetString(6),
                            Status = reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8),
                            CreatedBy = reader.IsDBNull(9) ? (long?)null : reader.GetInt64(9)
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

        // GET: api/Deviations/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeviation(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    SELECT id, batch_id, step_name, parameter_name, planned_value, actual_value, 
                           severity, status, created_at, created_by
                    FROM deviations WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return Ok(new
                        {
                            success = true,
                            data = new
                            {
                                Id = reader.GetInt64(0),
                                BatchId = reader.GetInt64(1),
                                StepName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                ParameterName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                PlannedValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                                ActualValue = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Severity = reader.GetString(6),
                                Status = reader.GetString(7),
                                CreatedAt = reader.GetDateTime(8),
                                CreatedBy = reader.IsDBNull(9) ? (long?)null : reader.GetInt64(9)
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Отклонение не найдено" });
        }

        // POST: api/Deviations
        [HttpPost]
        public async Task<IActionResult> CreateDeviation([FromBody] DeviationDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    INSERT INTO deviations (batch_id, step_name, parameter_name, planned_value, actual_value, severity, status, created_at, created_by)
                    VALUES (@batchId, @stepName, @paramName, @planned, @actual, @severity, 'open', GETDATE(), @createdBy);
                    SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@batchId", dto.BatchId);
                cmd.Parameters.AddWithValue("@stepName", (object)dto.StepName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@paramName", (object)dto.ParameterName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@planned", (object)dto.PlannedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@actual", (object)dto.ActualValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@severity", dto.Severity ?? "medium");
                cmd.Parameters.AddWithValue("@createdBy", dto.CreatedBy);

                // Исправление: SCOPE_IDENTITY() возвращает decimal, конвертируем в long
                var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, deviationId = newId, message = "Отклонение зарегистрировано" });
            }
        }

        // PUT: api/Deviations/{id}/resolve
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> ResolveDeviation(long id, [FromBody] ResolveDeviationDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM deviations WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                int exists = (int)await checkCmd.ExecuteScalarAsync();
                if (exists == 0)
                    return NotFound(new { success = false, message = "Отклонение не найдено" });

                // В твоей таблице нет resolved_at и resolved_by, обновляем только статус
                var cmd = new SqlCommand("UPDATE deviations SET status = 'resolved' WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { success = true, message = "Отклонение закрыто" });
            }
        }
    }

    // DTO для создания отклонения
    public class DeviationDto
    {
        public long BatchId { get; set; }
        public string StepName { get; set; }
        public string ParameterName { get; set; }
        public string PlannedValue { get; set; }
        public string ActualValue { get; set; }
        public string Severity { get; set; }
        public long CreatedBy { get; set; }
    }

    // DTO для закрытия отклонения
    public class ResolveDeviationDto
    {
        public long ResolvedBy { get; set; }
        public string ResolutionComment { get; set; }
    }
}