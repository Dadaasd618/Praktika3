using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AgroControlAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StatusesController : ControllerBase
    {
        [HttpGet("batch-statuses")]
        public IActionResult GetBatchStatuses()
        {
            var statuses = new[]
            {
                new { value = "planned", label = "Планируется", color = "gray" },
                new { value = "running", label = "Выполняется", color = "blue" },
                new { value = "quality_control", label = "Контроль качества", color = "yellow" },
                new { value = "completed", label = "Завершена", color = "green" },
                new { value = "blocked", label = "Заблокирована", color = "red" },
                new { value = "cancelled", label = "Отменена", color = "darkgray" }
            };
            return Ok(new { success = true, data = statuses });
        }

        [HttpGet("recipe-statuses")]
        public IActionResult GetRecipeStatuses()
        {
            var statuses = new[]
            {
                new { value = "draft", label = "Черновик", color = "gray" },
                new { value = "approved", label = "Согласована", color = "yellow" },
                new { value = "active", label = "Действующая", color = "green" },
                new { value = "archived", label = "Архивная", color = "darkgray" }
            };
            return Ok(new { success = true, data = statuses });
        }

        [HttpGet("techcard-statuses")]
        public IActionResult GetTechCardStatuses()
        {
            var statuses = new[]
            {
                new { value = "draft", label = "Черновик", color = "gray" },
                new { value = "active", label = "Действующая", color = "green" },
                new { value = "archived", label = "Архивная", color = "darkgray" }
            };
            return Ok(new { success = true, data = statuses });
        }

        [HttpGet("lab-test-statuses")]
        public IActionResult GetLabTestStatuses()
        {
            var statuses = new[]
            {
                new { value = "created", label = "Создано", color = "gray" },
                new { value = "in_progress", label = "В процессе", color = "blue" },
                new { value = "completed", label = "Завершено", color = "green" }
            };
            return Ok(new { success = true, data = statuses });
        }

        [HttpGet("deviation-statuses")]
        public IActionResult GetDeviationStatuses()
        {
            var statuses = new[]
            {
                new { value = "open", label = "Открыто", color = "red" },
                new { value = "resolved", label = "Решено", color = "green" }
            };
            return Ok(new { success = true, data = statuses });
        }

        [HttpGet("deviation-severities")]
        public IActionResult GetDeviationSeverities()
        {
            var severities = new[]
            {
                new { value = "low", label = "Низкая", color = "gray" },
                new { value = "medium", label = "Средняя", color = "yellow" },
                new { value = "high", label = "Высокая", color = "red" },
                new { value = "critical", label = "Критическая", color = "darkred" }
            };
            return Ok(new { success = true, data = severities });
        }
    }
}