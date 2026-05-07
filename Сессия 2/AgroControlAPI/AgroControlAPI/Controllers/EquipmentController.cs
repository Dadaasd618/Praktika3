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
    public class EquipmentController : ControllerBase
    {
        private readonly string _connectionString;

        public EquipmentController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var list = new List<object>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var countCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM equipment
                    WHERE (@type IS NULL OR type = @type)", conn);
                countCmd.Parameters.AddWithValue("@type", (object)type ?? DBNull.Value);
                totalCount = (int)await countCmd.ExecuteScalarAsync();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                    SELECT id, code, name, type, location 
                    FROM equipment
                    WHERE (@type IS NULL OR type = @type)
                    ORDER BY id
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@type", (object)type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@offset", offset);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            Code = reader.GetString(1),
                            Name = reader.GetString(2),
                            Type = reader.GetString(3),
                            Location = reader.GetString(4)
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
        public async Task<IActionResult> GetById(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, code, name, type, location FROM equipment WHERE id = @id", conn);
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
                                Code = reader.GetString(1),
                                Name = reader.GetString(2),
                                Type = reader.GetString(3),
                                Location = reader.GetString(4)
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Оборудование не найдено" });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEquipmentDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    INSERT INTO equipment (code, name, type, location) 
                    OUTPUT INSERTED.id
                    VALUES (@code, @name, @type, @location)", conn);
                cmd.Parameters.AddWithValue("@code", dto.Code);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@type", (object)dto.Type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object)dto.Location ?? DBNull.Value);

                // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, id = newId, message = "Оборудование добавлено" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateEquipmentDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    UPDATE equipment 
                    SET code = @code, name = @name, type = @type, location = @location 
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@code", dto.Code);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@type", (object)dto.Type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object)dto.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Оборудование не найдено" });
                return Ok(new { success = true, message = "Оборудование обновлено" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("DELETE FROM equipment WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Оборудование не найдено" });
                return Ok(new { success = true, message = "Оборудование удалено" });
            }
        }
    }

    public class CreateEquipmentDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
    }
}