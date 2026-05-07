using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly string _connectionString;

        public NotificationsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(long userId)
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, title, message, is_read, created_at FROM notifications WHERE user_id = @uid ORDER BY created_at DESC", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            Title = reader.GetString(1),
                            Message = reader.GetString(2),
                            IsRead = reader.GetBoolean(3),
                            CreatedAt = reader.GetDateTime(4)
                        });
                    }
                }
            }
            return Ok(list);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE notifications SET is_read = 1 WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true });
            }
        }
    }
}