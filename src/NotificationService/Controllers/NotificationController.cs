using Microsoft.AspNetCore.Mvc;
using NotificationService.Core.Interfaces;
using NotificationService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace NotificationService.Controllers
{
    /// <summary>
    /// Controller for viewing system notification logs.
    /// Provides read-only access to events captured from Kafka (order creation, drink ready).
    /// Logs are created automatically by the Kafka consumer — no manual creation endpoint.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Retrieves all logged notifications ordered by time.
        /// </summary>
        /// <returns>A list of logs.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Log>>> GetAllLogs()
        {
            var logs = await _notificationService.GetAllLogsAsync();
            return Ok(logs);
        }

        /// <summary>
        /// Retrieves a specific notification log by its unique identifier.
        /// </summary>
        /// <param name="id">The GUID of the log entry.</param>
        /// <returns>The log entry or 404 if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Log>> GetLogById(Guid id)
        {
            var log = await _notificationService.GetLogByIdAsync(id);

            if (log == null)
            {
                return NotFound();
            }

            return Ok(log);
        }

        /// <summary>
        /// Retrieves all notification logs for a specific order.
        /// </summary>
        /// <param name="orderId">The GUID of the order.</param>
        /// <returns>A list of logs related to the order.</returns>
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<Log>>> GetLogsByOrderId(Guid orderId)
        {
            var logs = await _notificationService.GetLogsByOrderIdAsync(orderId);
            return Ok(logs);
        }
    }
}
