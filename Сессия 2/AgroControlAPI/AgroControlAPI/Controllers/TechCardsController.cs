using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AgroControlAPI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgroControlAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechCardsController : ControllerBase
    {
        private readonly string _connectionString;

        public TechCardsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetTechCards()
        {
            var list = new List<object>();
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, product_id, recipe_id, version, status, is_active FROM tech_cards", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            Id = reader.GetInt64(0),
                            ProductId = reader.GetInt64(1),
                            RecipeId = reader.IsDBNull(2) ? (long?)null : reader.GetInt64(2),
                            Version = reader.GetString(3),
                            Status = reader.GetString(4),
                            IsActive = reader.GetBoolean(5)
                        });
                    }
                }
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTechCard(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT id, product_id, recipe_id, version, status, is_active FROM tech_cards WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var techCardId = reader.GetInt64(0);
                        var productId = reader.GetInt64(1);
                        var recipeId = reader.IsDBNull(2) ? (long?)null : reader.GetInt64(2);
                        var version = reader.GetString(3);
                        var status = reader.GetString(4);
                        var isActive = reader.GetBoolean(5);

                        var steps = new List<object>();
                        await reader.CloseAsync();

                        var stepCmd = new SqlCommand("SELECT id, step_order, name, step_type, planned_min, planned_max, unit, is_mandatory, instruction, duration_min FROM tech_steps WHERE tech_card_id = @id ORDER BY step_order", conn);
                        stepCmd.Parameters.AddWithValue("@id", techCardId);
                        using (var stepReader = await stepCmd.ExecuteReaderAsync())
                        {
                            while (await stepReader.ReadAsync())
                            {
                                steps.Add(new
                                {
                                    Id = stepReader.GetInt64(0),
                                    StepOrder = stepReader.GetInt32(1),
                                    Name = stepReader.GetString(2),
                                    StepType = stepReader.GetString(3),
                                    PlannedMin = stepReader.IsDBNull(4) ? (decimal?)null : stepReader.GetDecimal(4),
                                    PlannedMax = stepReader.IsDBNull(5) ? (decimal?)null : stepReader.GetDecimal(5),
                                    Unit = stepReader.GetString(6),
                                    IsMandatory = stepReader.GetBoolean(7),
                                    Instruction = stepReader.IsDBNull(8) ? null : stepReader.GetString(8),
                                    DurationMin = stepReader.IsDBNull(9) ? (int?)null : stepReader.GetInt32(9)
                                });
                            }
                        }

                        return Ok(new
                        {
                            Id = techCardId,
                            ProductId = productId,
                            RecipeId = recipeId,
                            Version = version,
                            Status = status,
                            IsActive = isActive,
                            Steps = steps
                        });
                    }
                }
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTechCard([FromBody] TechCardDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var cmd = new SqlCommand("INSERT INTO tech_cards (product_id, recipe_id, version, status, is_active, created_at) OUTPUT INSERTED.id VALUES (@pid, @rid, @ver, 'draft', 0, GETDATE())", conn, trans);
                    cmd.Parameters.AddWithValue("@pid", dto.ProductId);
                    cmd.Parameters.AddWithValue("@rid", (object)dto.RecipeId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ver", dto.Version);
                    var techCardId = (long)await cmd.ExecuteScalarAsync();

                    foreach (var step in dto.Steps)
                    {
                        var stepCmd = new SqlCommand("INSERT INTO tech_steps (tech_card_id, step_order, name, step_type, planned_min, planned_max, unit, is_mandatory, instruction, duration_min) VALUES (@tcid, @ord, @name, @type, @min, @max, @unit, @man, @instr, @dur)", conn, trans);
                        stepCmd.Parameters.AddWithValue("@tcid", techCardId);
                        stepCmd.Parameters.AddWithValue("@ord", step.StepOrder);
                        stepCmd.Parameters.AddWithValue("@name", step.Name);
                        stepCmd.Parameters.AddWithValue("@type", step.StepType ?? (object)DBNull.Value);
                        stepCmd.Parameters.AddWithValue("@min", (object)step.PlannedMin ?? DBNull.Value);
                        stepCmd.Parameters.AddWithValue("@max", (object)step.PlannedMax ?? DBNull.Value);
                        stepCmd.Parameters.AddWithValue("@unit", step.Unit ?? (object)DBNull.Value);
                        stepCmd.Parameters.AddWithValue("@man", step.IsMandatory);
                        stepCmd.Parameters.AddWithValue("@instr", step.Instruction ?? (object)DBNull.Value);
                        stepCmd.Parameters.AddWithValue("@dur", (object)step.DurationMin ?? DBNull.Value);
                        await stepCmd.ExecuteNonQueryAsync();
                    }

                    trans.Commit();
                    return Ok(new { success = true, techCardId });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTechCard(long id, [FromBody] TechCardDto dto)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var checkCmd = new SqlCommand("SELECT id FROM tech_cards WHERE id = @id", conn, trans);
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists == null)
                        return NotFound(new { success = false, message = "Техкарта не найдена" });

                    var updateCmd = new SqlCommand("UPDATE tech_cards SET version = @ver, recipe_id = @rid WHERE id = @id", conn, trans);
                    updateCmd.Parameters.AddWithValue("@ver", dto.Version);
                    updateCmd.Parameters.AddWithValue("@rid", (object)dto.RecipeId ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@id", id);
                    await updateCmd.ExecuteNonQueryAsync();

                    var delCmd = new SqlCommand("DELETE FROM tech_steps WHERE tech_card_id = @id", conn, trans);
                    delCmd.Parameters.AddWithValue("@id", id);
                    await delCmd.ExecuteNonQueryAsync();

                    foreach (var step in dto.Steps)
                    {
                        var insCmd = new SqlCommand("INSERT INTO tech_steps (tech_card_id, step_order, name, step_type, planned_min, planned_max, unit, is_mandatory, instruction, duration_min) VALUES (@tcid, @ord, @name, @type, @min, @max, @unit, @man, @instr, @dur)", conn, trans);
                        insCmd.Parameters.AddWithValue("@tcid", id);
                        insCmd.Parameters.AddWithValue("@ord", step.StepOrder);
                        insCmd.Parameters.AddWithValue("@name", step.Name);
                        insCmd.Parameters.AddWithValue("@type", step.StepType ?? (object)DBNull.Value);
                        insCmd.Parameters.AddWithValue("@min", (object)step.PlannedMin ?? DBNull.Value);
                        insCmd.Parameters.AddWithValue("@max", (object)step.PlannedMax ?? DBNull.Value);
                        insCmd.Parameters.AddWithValue("@unit", step.Unit ?? (object)DBNull.Value);
                        insCmd.Parameters.AddWithValue("@man", step.IsMandatory);
                        insCmd.Parameters.AddWithValue("@instr", step.Instruction ?? (object)DBNull.Value);
                        insCmd.Parameters.AddWithValue("@dur", (object)step.DurationMin ?? DBNull.Value);
                        await insCmd.ExecuteNonQueryAsync();
                    }

                    trans.Commit();
                    return Ok(new { success = true, message = "Техкарта обновлена" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ArchiveTechCard(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var checkCmd = new SqlCommand("SELECT id FROM tech_cards WHERE id = @id", conn, trans);
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists == null)
                        return NotFound(new { success = false, message = "Техкарта не найдена" });

                    var archiveCmd = new SqlCommand("UPDATE tech_cards SET status = 'archived', is_active = 0 WHERE id = @id", conn, trans);
                    archiveCmd.Parameters.AddWithValue("@id", id);
                    await archiveCmd.ExecuteNonQueryAsync();

                    trans.Commit();
                    return Ok(new { success = true, message = "Техкарта архивирована" });
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveTechCard(long id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                var trans = conn.BeginTransaction();
                try
                {
                    var prodCmd = new SqlCommand("SELECT product_id FROM tech_cards WHERE id = @id", conn, trans);
                    prodCmd.Parameters.AddWithValue("@id", id);
                    var productId = (long)await prodCmd.ExecuteScalarAsync();

                    var deactivateCmd = new SqlCommand("UPDATE tech_cards SET is_active = 0 WHERE product_id = @pid AND is_active = 1", conn, trans);
                    deactivateCmd.Parameters.AddWithValue("@pid", productId);
                    await deactivateCmd.ExecuteNonQueryAsync();

                    var activateCmd = new SqlCommand("UPDATE tech_cards SET status = 'active', is_active = 1, approved_at = GETDATE() WHERE id = @id", conn, trans);
                    activateCmd.Parameters.AddWithValue("@id", id);
                    await activateCmd.ExecuteNonQueryAsync();

                    trans.Commit();
                    return Ok(new { success = true, message = "Техкарта утверждена" });
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