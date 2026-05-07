using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AgroControlAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly string _connectionString;

        public OrdersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, order_number, recipe_id, planned_quantity_kg, status, planned_start_date FROM production_orders", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            OrderNumber = reader.GetString(1),
                            RecipeId = reader.GetInt64(2),
                            PlannedQuantityKg = reader.GetDecimal(3),
                            Status = reader.GetString(4),
                            PlannedStartDate = reader.GetDateTime(5)
                        });
                    }
                }
            }
            return Ok(new { success = true, data = list });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, order_number, recipe_id, planned_quantity_kg, status, planned_start_date FROM production_orders WHERE id = @id", conn);
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
                                OrderNumber = reader.GetString(1),
                                RecipeId = reader.GetInt64(2),
                                PlannedQuantityKg = reader.GetDecimal(3),
                                Status = reader.GetString(4),
                                PlannedStartDate = reader.GetDateTime(5)
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Заказ не найден" });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] ProductionOrderDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("INSERT INTO production_orders (order_number, recipe_id, planned_quantity_kg, status, planned_start_date, created_at) VALUES (@num, @rid, @qty, 'planned', @date, GETDATE()); SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@num", dto.OrderNumber);
                cmd.Parameters.AddWithValue("@rid", dto.RecipeId);
                cmd.Parameters.AddWithValue("@qty", dto.PlannedQuantityKg);
                cmd.Parameters.AddWithValue("@date", dto.PlannedStartDate);

                // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                var newId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                return Ok(new { success = true, orderId = newId, message = "Заказ создан" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(long id, [FromBody] ProductionOrderDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT id FROM production_orders WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                    return NotFound(new { success = false, message = "Заказ не найден" });

                var cmd = new SqlCommand("UPDATE production_orders SET order_number = @num, recipe_id = @rid, planned_quantity_kg = @qty, planned_start_date = @date WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@num", dto.OrderNumber);
                cmd.Parameters.AddWithValue("@rid", dto.RecipeId);
                cmd.Parameters.AddWithValue("@qty", dto.PlannedQuantityKg);
                cmd.Parameters.AddWithValue("@date", dto.PlannedStartDate);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Заказ обновлён" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelOrder(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var checkCmd = new SqlCommand("SELECT id FROM production_orders WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                    return NotFound(new { success = false, message = "Заказ не найден" });

                var cmd = new SqlCommand("UPDATE production_orders SET status = 'cancelled' WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                return Ok(new { success = true, message = "Заказ отменён" });
            }
        }
    }
}