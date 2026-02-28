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
        private readonly BarService.Messaging.IKafkaProducer _kafkaProducer;

        public BarController(IBarService barService, BarService.Messaging.IKafkaProducer kafkaProducer)
        {
            _barService = barService;
            _kafkaProducer = kafkaProducer;
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

        [HttpPut("{id}/mark-ready")]
        public async Task<IActionResult> MarkAsReady(Guid id)
        {
            var drinkTask = await _barService.GetDrinkTaskByIdAsync(id);
            if (drinkTask == null)
            {
                return NotFound();
            }

            drinkTask.IsReady = true;
            await _barService.UpdateDrinkTaskAsync(drinkTask);

            // Notify via Kafka
            var readyEvent = new BarService.Events.DrinkReadyEvent
            {
                DrinkTaskId = drinkTask.Id,
                OrderId = drinkTask.OrderId,
                DrinkName = drinkTask.Name
            };

            await _kafkaProducer.ProduceAsync("drink-ready-events", drinkTask.Id.ToString(), readyEvent);

            return Ok(drinkTask);
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
