using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviationsController : ControllerBase
    {
        private readonly string _connectionString;

        public DeviationsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetDeviations()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, batch_id, step_name, parameter_name, planned_value, actual_value, severity, status, created_at FROM deviations ORDER BY created_at DESC", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            BatchId = reader.GetInt64(1),
                            StepName = reader.GetString(2),
                            ParameterName = reader.GetString(3),
                            PlannedValue = reader.GetString(4),
                            ActualValue = reader.GetString(5),
                            Severity = reader.GetString(6),
                            Status = reader.GetString(7),
                            CreatedAt = reader.GetDateTime(8)
                        });
                    }
                }
            }
            return Ok(list);
        }
    }
}