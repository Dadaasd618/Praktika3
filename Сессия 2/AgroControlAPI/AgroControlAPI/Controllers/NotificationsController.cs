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
    public class NotificationsController : ControllerBase
    {
        private readonly string _connectionString;

        public NotificationsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(
            long userId,
            [FromQuery] bool? isRead = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var list = new List<object>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var countCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM notifications 
                    WHERE user_id = @uid AND (@isRead IS NULL OR is_read = @isRead)", conn);
                countCmd.Parameters.AddWithValue("@uid", userId);
                countCmd.Parameters.AddWithValue("@isRead", (object)isRead ?? DBNull.Value);
                totalCount = (int)await countCmd.ExecuteScalarAsync();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                    SELECT id, title, message, is_read, created_at 
                    FROM notifications 
                    WHERE user_id = @uid AND (@isRead IS NULL OR is_read = @isRead)
                    ORDER BY created_at DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@isRead", (object)isRead ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

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

            return Ok(new
            {
                success = true,
                data = list,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    INSERT INTO notifications (user_id, title, message, is_read, created_at)
                    VALUES (@userId, @title, @message, 0, GETDATE());
                    SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@userId", dto.UserId);
                cmd.Parameters.AddWithValue("@title", dto.Title);
                cmd.Parameters.AddWithValue("@message", dto.Message);
                var newId = (long)await cmd.ExecuteScalarAsync();
                return Ok(new { success = true, notificationId = newId, message = "Уведомление отправлено" });
            }
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE notifications SET is_read = 1 WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Уведомление не найдено" });
                return Ok(new { success = true, message = "Уведомление прочитано" });
            }
        }

        [HttpPost("mark-all-read/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(long userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE notifications SET is_read = 1 WHERE user_id = @uid AND is_read = 0", conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                int rows = await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, updatedCount = rows, message = "Все уведомления отмечены как прочитанные" });
            }
        }
    }

    public class SendNotificationDto
    {
        public long UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}