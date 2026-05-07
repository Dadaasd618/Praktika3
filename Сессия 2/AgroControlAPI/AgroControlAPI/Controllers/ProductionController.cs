using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly string _connectionString;

        public ProductsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, code, name, type, form, status, created_at FROM products", conn);
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
                            Form = reader.GetString(4),
                            Status = reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        });
                    }
                }
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, code, name, type, form, status, created_at FROM products WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return Ok(new
                        {
                            Id = reader.GetInt64(0),
                            Code = reader.GetString(1),
                            Name = reader.GetString(2),
                            Type = reader.GetString(3),
                            Form = reader.GetString(4),
                            Status = reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        });
                    }
                }
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("INSERT INTO products (code, name, type, form, status, created_at) VALUES (@code, @name, @type, @form, 'active', GETDATE()); SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@code", dto.Code);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@type", dto.Type ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@form", dto.Form ?? (object)DBNull.Value);
                var newId = (long)await cmd.ExecuteScalarAsync();
                return Ok(new { success = true, productId = newId });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] CreateProductDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT id FROM products WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                    return NotFound(new { success = false, message = "Продукт не найден" });

                var cmd = new SqlCommand("UPDATE products SET code = @code, name = @name, type = @type, form = @form WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@code", dto.Code);
                cmd.Parameters.AddWithValue("@name", dto.Name);
                cmd.Parameters.AddWithValue("@type", dto.Type ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@form", dto.Form ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Продукт обновлён" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ArchiveProduct(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT id FROM products WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                    return NotFound(new { success = false, message = "Продукт не найден" });

                var cmd = new SqlCommand("UPDATE products SET status = 'archived' WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Продукт архивирован" });
            }
        }
    }

    public class CreateProductDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Form { get; set; }
    }
}