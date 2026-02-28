using Microsoft.AspNetCore.Mvc;
using BarService.Core.Interfaces;
using BarService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BarService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarController : ControllerBase
    {
        private readonly IBarService _barService;

        public BarController(IBarService barService)
        {
            _barService = barService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DrinkTask>>> GetAllDrinkTasks()
        {
            var drinkTasks = await _barService.GetAllDrinkTasksAsync();
            return Ok(drinkTasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DrinkTask>> GetDrinkTaskById(Guid id)
        {
            var drinkTask = await _barService.GetDrinkTaskByIdAsync(id);

            if (drinkTask == null)
            {
                return NotFound();
            }

            return Ok(drinkTask);
        }

        [HttpPost]
        public async Task<ActionResult<DrinkTask>> CreateDrinkTask(DrinkTask drinkTask)
        {
            var createdDrinkTask = await _barService.CreateDrinkTaskAsync(drinkTask);

            return CreatedAtAction(nameof(GetDrinkTaskById), new { id = createdDrinkTask.Id }, createdDrinkTask);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDrinkTask(Guid id, DrinkTask drinkTask)
        {
            if (id != drinkTask.Id)
            {
                return BadRequest();
            }

            var existingDrinkTask = await _barService.GetDrinkTaskByIdAsync(id);
            if (existingDrinkTask == null)
            {
                return NotFound();
            }

            await _barService.UpdateDrinkTaskAsync(drinkTask);

            // TODO: Here we will later publish a message to Kafka if IsReady is set to true.

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDrinkTask(Guid id)
        {
            var existingDrinkTask = await _barService.GetDrinkTaskByIdAsync(id);
            if (existingDrinkTask == null)
            {
                return NotFound();
            }

            await _barService.DeleteDrinkTaskAsync(id);

            return NoContent();
        }
    }
}
