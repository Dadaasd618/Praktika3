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
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString;

        public UsersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var list = new List<object>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var countCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM users
                    WHERE (@role IS NULL OR role = @role)
                      AND (@isActive IS NULL OR is_active = @isActive)", conn);
                countCmd.Parameters.AddWithValue("@role", (object)role ?? DBNull.Value);
                countCmd.Parameters.AddWithValue("@isActive", (object)isActive ?? DBNull.Value);
                totalCount = (int)await countCmd.ExecuteScalarAsync();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                    SELECT id, username, full_name, role, email, phone, department, is_active, last_login, created_at
                    FROM users
                    WHERE (@role IS NULL OR role = @role)
                      AND (@isActive IS NULL OR is_active = @isActive)
                    ORDER BY id
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@role", (object)role ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isActive", (object)isActive ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            Username = reader.GetString(1),
                            FullName = reader.GetString(2),
                            Role = reader.GetString(3),
                            Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Phone = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Department = reader.IsDBNull(6) ? null : reader.GetString(6),
                            IsActive = reader.GetBoolean(7),
                            LastLogin = reader.IsDBNull(8) ? (System.DateTime?)null : reader.GetDateTime(8),
                            CreatedAt = reader.GetDateTime(9)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    SELECT id, username, full_name, role, email, phone, department, is_active, last_login, created_at
                    FROM users WHERE id = @id", conn);
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
                                Username = reader.GetString(1),
                                FullName = reader.GetString(2),
                                Role = reader.GetString(3),
                                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Phone = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Department = reader.IsDBNull(6) ? null : reader.GetString(6),
                                IsActive = reader.GetBoolean(7),
                                LastLogin = reader.IsDBNull(8) ? (System.DateTime?)null : reader.GetDateTime(8),
                                CreatedAt = reader.GetDateTime(9)
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Пользователь не найден" });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE username = @username", conn);
                checkCmd.Parameters.AddWithValue("@username", dto.Username);
                int exists = (int)await checkCmd.ExecuteScalarAsync();
                if (exists > 0)
                    return BadRequest(new { success = false, message = "Пользователь с таким логином уже существует" });

                var cmd = new SqlCommand(@"
                    INSERT INTO users (username, password_hash, full_name, role, email, phone, department, is_active, created_at)
                    OUTPUT INSERTED.id
                    VALUES (@username, @password, @fullName, @role, @email, @phone, @dept, 1, GETDATE())", conn);
                cmd.Parameters.AddWithValue("@username", dto.Username);
                cmd.Parameters.AddWithValue("@password", dto.Password);
                cmd.Parameters.AddWithValue("@fullName", dto.FullName);
                cmd.Parameters.AddWithValue("@role", dto.Role);
                cmd.Parameters.AddWithValue("@email", (object)dto.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", (object)dto.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", (object)dto.Department ?? DBNull.Value);

                // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, id = newId, message = "Пользователь создан" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                int exists = (int)await checkCmd.ExecuteScalarAsync();
                if (exists == 0)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                var cmd = new SqlCommand(@"
                    UPDATE users 
                    SET full_name = @fullName, 
                        role = @role, 
                        email = @email, 
                        phone = @phone, 
                        department = @dept
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@fullName", dto.FullName);
                cmd.Parameters.AddWithValue("@role", dto.Role);
                cmd.Parameters.AddWithValue("@email", (object)dto.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@phone", (object)dto.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", (object)dto.Department ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();

                if (!string.IsNullOrEmpty(dto.Password))
                {
                    var pwdCmd = new SqlCommand("UPDATE users SET password_hash = @pwd WHERE id = @id", conn);
                    pwdCmd.Parameters.AddWithValue("@pwd", dto.Password);
                    pwdCmd.Parameters.AddWithValue("@id", id);
                    await pwdCmd.ExecuteNonQueryAsync();
                }

                return Ok(new { success = true, message = "Пользователь обновлён" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateUser(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                int exists = (int)await checkCmd.ExecuteScalarAsync();
                if (exists == 0)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                var cmd = new SqlCommand("UPDATE users SET is_active = 0 WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Пользователь деактивирован" });
            }
        }

        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateUser(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM users WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                int exists = (int)await checkCmd.ExecuteScalarAsync();
                if (exists == 0)
                    return NotFound(new { success = false, message = "Пользователь не найден" });

                var cmd = new SqlCommand("UPDATE users SET is_active = 1 WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Пользователь активирован" });
            }
        }
    }

    public class CreateUserDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
    }

    public class UpdateUserDto
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
        public string Password { get; set; }
    }
}