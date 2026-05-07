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
    public class RolesController : ControllerBase
    {
        private readonly string _connectionString;

        public RolesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, name, description FROM roles", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            Name = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                }
            }
            return Ok(new { success = true, data = list });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, name, description FROM roles WHERE id = @id", conn);
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
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Роль не найдена" });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    INSERT INTO roles (name, description) 
                    OUTPUT INSERTED.id
                    VALUES (@name, @desc)", conn);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@desc", (object)dto.Description ?? DBNull.Value);

                // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, id = newId, message = "Роль создана" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(long id, [FromBody] CreateRoleDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    UPDATE roles 
                    SET name = @name, description = @desc 
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@desc", (object)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Роль не найдена" });
                return Ok(new { success = true, message = "Роль обновлена" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("DELETE FROM roles WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Роль не найдена" });
                return Ok(new { success = true, message = "Роль удалена" });
            }
        }
    }

    public class CreateRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}