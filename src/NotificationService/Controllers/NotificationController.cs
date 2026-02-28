using Microsoft.AspNetCore.Mvc;
using NotificationService.Core.Interfaces;
using NotificationService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace NotificationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Log>>> GetAllLogs()
        {
            var logs = await _notificationService.GetAllLogsAsync();
            return Ok(logs);
        }

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

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<Log>>> GetLogsByOrderId(Guid orderId)
        {
            var logs = await _notificationService.GetLogsByOrderIdAsync(orderId);
            return Ok(logs);
        }

        [HttpPost]
        public async Task<ActionResult<Log>> CreateLog(Log log)
        {
            var createdLog = await _notificationService.CreateLogAsync(log);

            return CreatedAtAction(nameof(GetLogById), new { id = createdLog.Id }, createdLog);
        }
    }
}
