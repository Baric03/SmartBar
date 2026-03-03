using Microsoft.AspNetCore.Mvc;
using BarService.Core.Interfaces;
using BarService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BarService.Controllers
{
    /// <summary>
    /// Controller for managing bar operations and drink preparation tasks.
    /// Provides read-only access to drink tasks and the ability to mark them as ready.
    /// Drink tasks are created automatically via Kafka when new orders are placed.
    /// </summary>
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

        /// <summary>
        /// Retrieves all current drink preparation tasks.
        /// </summary>
        /// <returns>A list of drink tasks.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DrinkTask>>> GetAllDrinkTasks()
        {
            var drinkTasks = await _barService.GetAllDrinkTasksAsync();
            return Ok(drinkTasks);
        }

        /// <summary>
        /// Retrieves a specific drink task by its unique identifier.
        /// </summary>
        /// <param name="id">The GUID of the drink task.</param>
        /// <returns>The drink task or 404 if not found.</returns>
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

        /// <summary>
        /// Marks a specific drink task as ready and publishes a completion event to Kafka.
        /// This triggers a notification and updates the order status in OrderService.
        /// </summary>
        /// <param name="id">The GUID of the drink task.</param>
        /// <returns>The updated drink task.</returns>
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

            // Check if ALL drink tasks for this order are now ready
            var allTasks = await _barService.GetDrinkTasksByOrderIdAsync(drinkTask.OrderId);
            var allReady = allTasks.All(t => t.IsReady);

            // Notify via Kafka — consumed by NotificationService and OrderService
            var readyEvent = new BarService.Events.DrinkReadyEvent
            {
                DrinkTaskId = drinkTask.Id,
                OrderId = drinkTask.OrderId,
                DrinkName = drinkTask.Name,
                AllDrinksReady = allReady
            };

            await _kafkaProducer.ProduceAsync("drink-ready-events", drinkTask.Id.ToString(), readyEvent);

            return Ok(drinkTask);
        }
    }
}
