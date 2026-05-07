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
    public class RawMaterialsController : ControllerBase
    {
        private readonly string _connectionString;

        public RawMaterialsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string category = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var list = new List<object>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var countCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM raw_materials
                    WHERE (@category IS NULL OR category = @category)", conn);
                countCmd.Parameters.AddWithValue("@category", (object)category ?? DBNull.Value);
                totalCount = (int)await countCmd.ExecuteScalarAsync();

                var offset = (page - 1) * pageSize;
                var cmd = new SqlCommand(@"
                    SELECT id, code, name, category, unit, status 
                    FROM raw_materials
                    WHERE (@category IS NULL OR category = @category)
                    ORDER BY id
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);
                cmd.Parameters.AddWithValue("@category", (object)category ?? DBNull.Value);
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
                            Category = reader.GetString(3),
                            Unit = reader.GetString(4),
                            Status = reader.GetString(5)
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
                var cmd = new SqlCommand("SELECT id, code, name, category, unit, status FROM raw_materials WHERE id = @id", conn);
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
                                Category = reader.GetString(3),
                                Unit = reader.GetString(4),
                                Status = reader.GetString(5)
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Сырьё не найдено" });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRawMaterialDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    INSERT INTO raw_materials (code, name, category, unit, status) 
                    OUTPUT INSERTED.id
                    VALUES (@code, @name, @cat, @unit, 'active')", conn);
                cmd.Parameters.AddWithValue("@code", dto.Code);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@cat", (object)dto.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unit", (object)dto.Unit ?? DBNull.Value);

                // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, id = newId, message = "Сырьё добавлено" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateRawMaterialDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    UPDATE raw_materials 
                    SET code = @code, name = @name, category = @cat, unit = @unit 
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@code", dto.Code);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@cat", (object)dto.Category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unit", (object)dto.Unit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Сырьё не найдено" });
                return Ok(new { success = true, message = "Сырьё обновлено" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Archive(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE raw_materials SET status = 'archived' WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound(new { success = false, message = "Сырьё не найдено" });
                return Ok(new { success = true, message = "Сырьё архивировано" });
            }
        }
    }

    public class CreateRawMaterialDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
    }
}