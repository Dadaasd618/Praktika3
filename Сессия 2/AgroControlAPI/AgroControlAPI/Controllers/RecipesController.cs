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
    public class RecipesController : ControllerBase
    {
        private readonly string _connectionString;

        public RecipesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetRecipes()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, product_id, version, status, is_active, created_at FROM recipes", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            ProductId = reader.GetInt64(1),
                            Version = reader.GetString(2),
                            Status = reader.GetString(3),
                            IsActive = reader.GetBoolean(4),
                            CreatedAt = reader.GetDateTime(5)
                        });
                    }
                }
            }
            return Ok(new { success = true, data = list });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, product_id, version, status, is_active, created_at FROM recipes WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var components = new List<object>();
                        var recipeId = reader.GetInt64(0);
                        var productId = reader.GetInt64(1);
                        var version = reader.GetString(2);
                        var status = reader.GetString(3);
                        var isActive = reader.GetBoolean(4);
                        var createdAt = reader.GetDateTime(5);

                        await reader.CloseAsync();

                        var cmdComp = new SqlCommand("SELECT id, raw_material_id, percentage, tolerance, load_order FROM recipe_components WHERE recipe_id = @id ORDER BY load_order", conn);
                        cmdComp.Parameters.AddWithValue("@id", recipeId);
                        using (var compReader = await cmdComp.ExecuteReaderAsync())
                        {
                            while (await compReader.ReadAsync())
                            {
                                components.Add(new
                                {
                                    Id = compReader.GetInt64(0),
                                    RawMaterialId = compReader.GetInt64(1),
                                    Percentage = compReader.GetDecimal(2),
                                    Tolerance = compReader.GetDecimal(3),
                                    LoadOrder = compReader.GetInt32(4)
                                });
                            }
                        }

                        return Ok(new
                        {
                            success = true,
                            data = new
                            {
                                Id = recipeId,
                                ProductId = productId,
                                Version = version,
                                Status = status,
                                IsActive = isActive,
                                CreatedAt = createdAt,
                                Components = components
                            }
                        });
                    }
                }
            }
            return NotFound(new { success = false, message = "Рецептура не найдена" });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromBody] RecipeDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var cmd = new SqlCommand("INSERT INTO recipes (product_id, version, status, is_active, created_at) OUTPUT INSERTED.id VALUES (@pid, @ver, 'draft', 0, GETDATE())", conn, trans);
                    cmd.Parameters.AddWithValue("@pid", dto.ProductId);
                    cmd.Parameters.AddWithValue("@ver", dto.Version);

                    // ИСПРАВЛЕНО: Convert.ToInt64 вместо прямого каста
                    var recipeId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

                    foreach (var comp in dto.Components)
                    {
                        var cmdComp = new SqlCommand("INSERT INTO recipe_components (recipe_id, raw_material_id, percentage, tolerance, load_order) VALUES (@rid, @rmid, @pct, @tol, @ord)", conn, trans);
                        cmdComp.Parameters.AddWithValue("@rid", recipeId);
                        cmdComp.Parameters.AddWithValue("@rmid", comp.RawMaterialId);
                        cmdComp.Parameters.AddWithValue("@pct", comp.Percentage);
                        cmdComp.Parameters.AddWithValue("@tol", comp.Tolerance);
                        cmdComp.Parameters.AddWithValue("@ord", comp.LoadOrder);
                        await cmdComp.ExecuteNonQueryAsync();
                    }

                    trans.Commit();
                    return Ok(new { success = true, recipeId, message = "Рецептура создана" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(long id, [FromBody] RecipeDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var checkCmd = new SqlCommand("SELECT id FROM recipes WHERE id = @id", conn, trans);
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists == null)
                        return NotFound(new { success = false, message = "Рецептура не найдена" });

                    var updateCmd = new SqlCommand("UPDATE recipes SET version = @ver WHERE id = @id", conn, trans);
                    updateCmd.Parameters.AddWithValue("@ver", dto.Version);
                    updateCmd.Parameters.AddWithValue("@id", id);
                    await updateCmd.ExecuteNonQueryAsync();

                    var delCmd = new SqlCommand("DELETE FROM recipe_components WHERE recipe_id = @id", conn, trans);
                    delCmd.Parameters.AddWithValue("@id", id);
                    await delCmd.ExecuteNonQueryAsync();

                    foreach (var comp in dto.Components)
                    {
                        var insCmd = new SqlCommand("INSERT INTO recipe_components (recipe_id, raw_material_id, percentage, tolerance, load_order) VALUES (@rid, @rmid, @pct, @tol, @ord)", conn, trans);
                        insCmd.Parameters.AddWithValue("@rid", id);
                        insCmd.Parameters.AddWithValue("@rmid", comp.RawMaterialId);
                        insCmd.Parameters.AddWithValue("@pct", comp.Percentage);
                        insCmd.Parameters.AddWithValue("@tol", comp.Tolerance);
                        insCmd.Parameters.AddWithValue("@ord", comp.LoadOrder);
                        await insCmd.ExecuteNonQueryAsync();
                    }

                    trans.Commit();
                    return Ok(new { success = true, message = "Рецептура обновлена" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ArchiveRecipe(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var checkCmd = new SqlCommand("SELECT id FROM recipes WHERE id = @id", conn, trans);
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists == null)
                        return NotFound(new { success = false, message = "Рецептура не найдена" });

                    var archiveCmd = new SqlCommand("UPDATE recipes SET status = 'archived', is_active = 0 WHERE id = @id", conn, trans);
                    archiveCmd.Parameters.AddWithValue("@id", id);
                    await archiveCmd.ExecuteNonQueryAsync();

                    trans.Commit();
                    return Ok(new { success = true, message = "Рецептура архивирована" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveRecipe(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var sumCmd = new SqlCommand("SELECT SUM(percentage) FROM recipe_components WHERE recipe_id = @id", conn, trans);
                    sumCmd.Parameters.AddWithValue("@id", id);
                    var sum = await sumCmd.ExecuteScalarAsync() as decimal?;
                    if (sum != 100)
                        return BadRequest(new { success = false, message = $"Сумма долей компонентов должна быть 100% (сейчас {sum}%)" });

                    var productCmd = new SqlCommand("SELECT product_id FROM recipes WHERE id = @id", conn, trans);
                    productCmd.Parameters.AddWithValue("@id", id);
                    var productId = (long)await productCmd.ExecuteScalarAsync();

                    var deactivateCmd = new SqlCommand("UPDATE recipes SET is_active = 0 WHERE product_id = @pid AND is_active = 1", conn, trans);
                    deactivateCmd.Parameters.AddWithValue("@pid", productId);
                    await deactivateCmd.ExecuteNonQueryAsync();

                    var activateCmd = new SqlCommand("UPDATE recipes SET status = 'active', is_active = 1, approved_at = GETDATE() WHERE id = @id", conn, trans);
                    activateCmd.Parameters.AddWithValue("@id", id);
                    await activateCmd.ExecuteNonQueryAsync();

                    trans.Commit();
                    return Ok(new { success = true, message = "Рецептура утверждена" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}