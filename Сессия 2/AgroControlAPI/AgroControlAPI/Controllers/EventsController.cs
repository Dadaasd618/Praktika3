using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly string _connectionString;

        public EventsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, event_type, object_type, object_id, description, created_at, created_by FROM audit_events ORDER BY created_at DESC", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            EventType = reader.GetString(1),
                            ObjectType = reader.GetString(2),
                            ObjectId = reader.GetInt64(3),
                            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                            CreatedAt = reader.GetDateTime(5),
                            CreatedBy = reader.GetInt64(6)
                        });
                    }
                }
            }
            return Ok(list);
        }
    }
}