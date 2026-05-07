using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReferenceController : ControllerBase
    {
        private readonly string _connectionString;

        public ReferenceController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, code, name, type, form, status FROM products", conn);
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
                            Status = reader.GetString(5)
                        });
                    }
                }
            }
            return Ok(list);
        }

        [HttpGet("raw-materials")]
        public async Task<IActionResult> GetRawMaterials()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, code, name, category, unit, status FROM raw_materials", conn);
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
            return Ok(list);
        }

        [HttpGet("equipment")]
        public async Task<IActionResult> GetEquipment()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, code, name, type, location FROM equipment", conn);
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
            return Ok(list);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, username, full_name, role, department FROM users WHERE is_active = 1", conn);
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
                            Department = reader.GetString(4)
                        });
                    }
                }
            }
            return Ok(list);
        }
    }
}