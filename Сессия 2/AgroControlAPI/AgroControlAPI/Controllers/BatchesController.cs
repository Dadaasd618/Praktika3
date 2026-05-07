using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BatchesController : ControllerBase
    {
        private readonly string _connectionString;

        public BatchesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetBatches()
        {
            var list = new System.Collections.Generic.List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, batch_number, order_id, product_id, status, start_time, end_time FROM production_batches", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            BatchNumber = reader.GetString(1),
                            OrderId = reader.GetInt64(2),
                            ProductId = reader.GetInt64(3),
                            Status = reader.GetString(4),
                            StartTime = reader.IsDBNull(5) ? (System.DateTime?)null : reader.GetDateTime(5),
                            EndTime = reader.IsDBNull(6) ? (System.DateTime?)null : reader.GetDateTime(6)
                        });
                    }
                }
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBatch(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, batch_number, order_id, product_id, recipe_version_id, tech_card_id, status, start_time, end_time FROM production_batches WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var batchId = reader.GetInt64(0);
                        var batchNumber = reader.GetString(1);
                        var orderId = reader.GetInt64(2);
                        var productId = reader.GetInt64(3);
                        var recipeVersionId = reader.GetInt64(4);
                        var techCardId = reader.GetInt64(5);
                        var status = reader.GetString(6);
                        var startTime = reader.IsDBNull(7) ? (System.DateTime?)null : reader.GetDateTime(7);
                        var endTime = reader.IsDBNull(8) ? (System.DateTime?)null : reader.GetDateTime(8);

                        var steps = new System.Collections.Generic.List<object>();
                        await reader.CloseAsync();

                        var stepCmd = new SqlCommand("SELECT id, step_order, step_name, status, actual_value, deviation_flag FROM batch_step_actual WHERE batch_id = @id ORDER BY step_order", conn);
                        stepCmd.Parameters.AddWithValue("@id", batchId);
                        using (var stepReader = await stepCmd.ExecuteReaderAsync())
                        {
                            while (await stepReader.ReadAsync())
                            {
                                steps.Add(new
                                {
                                    Id = stepReader.GetInt64(0),
                                    StepOrder = stepReader.GetInt32(1),
                                    StepName = stepReader.GetString(2),
                                    Status = stepReader.GetString(3),
                                    ActualValue = stepReader.IsDBNull(4) ? (decimal?)null : stepReader.GetDecimal(4),
                                    DeviationFlag = stepReader.GetBoolean(5)
                                });
                            }
                        }

                        return Ok(new
                        {
                            Id = batchId,
                            BatchNumber = batchNumber,
                            OrderId = orderId,
                            ProductId = productId,
                            RecipeVersionId = recipeVersionId,
                            TechCardId = techCardId,
                            Status = status,
                            StartTime = startTime,
                            EndTime = endTime,
                            Steps = steps
                        });
                    }
                }
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBatch([FromBody] CreateBatchDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var orderCmd = new SqlCommand("SELECT recipe_id FROM production_orders WHERE id = @oid", conn, trans);
                    orderCmd.Parameters.AddWithValue("@oid", dto.OrderId);
                    var recipeId = (long)await orderCmd.ExecuteScalarAsync();

                    var prodCmd = new SqlCommand("SELECT product_id FROM recipes WHERE id = @rid", conn, trans);
                    prodCmd.Parameters.AddWithValue("@rid", recipeId);
                    var productId = (long)await prodCmd.ExecuteScalarAsync();

                    var techCmd = new SqlCommand("SELECT TOP 1 id FROM tech_cards WHERE product_id = @pid AND is_active = 1", conn, trans);
                    techCmd.Parameters.AddWithValue("@pid", productId);
                    var techCardId = (long)await techCmd.ExecuteScalarAsync();

                    var batchCmd = new SqlCommand("INSERT INTO production_batches (batch_number, order_id, product_id, recipe_version_id, tech_card_id, status, created_at) OUTPUT INSERTED.id VALUES (@num, @oid, @pid, @rid, @tcid, 'planned', GETDATE())", conn, trans);
                    batchCmd.Parameters.AddWithValue("@num", dto.BatchNumber);
                    batchCmd.Parameters.AddWithValue("@oid", dto.OrderId);
                    batchCmd.Parameters.AddWithValue("@pid", productId);
                    batchCmd.Parameters.AddWithValue("@rid", recipeId);
                    batchCmd.Parameters.AddWithValue("@tcid", techCardId);
                    var batchId = (long)await batchCmd.ExecuteScalarAsync();

                    var stepsCmd = new SqlCommand("INSERT INTO batch_step_actual (batch_id, step_order, step_name, status) SELECT @bid, step_order, name, 'pending' FROM tech_steps WHERE tech_card_id = @tcid ORDER BY step_order", conn, trans);
                    stepsCmd.Parameters.AddWithValue("@bid", batchId);
                    stepsCmd.Parameters.AddWithValue("@tcid", techCardId);
                    await stepsCmd.ExecuteNonQueryAsync();

                    trans.Commit();
                    return Ok(new { success = true, batchId });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
    }

    public class CreateBatchDto
    {
        public string BatchNumber { get; set; }
        public long OrderId { get; set; }
    }
}