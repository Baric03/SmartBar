using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NotificationService.Core.Interfaces;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Core.Services
{
    /// <summary>
    /// Service for managing notification logs.
    /// Stores events captured from the system for auditing and display.
    /// </summary>
    public class NotificationManagementService : INotificationService
    {
        private readonly NotificationDbContext _context;

        public NotificationManagementService(NotificationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Log>> GetAllLogsAsync()
        {
            return await _context.Logs.OrderByDescending(l => l.SentAt).ToListAsync();
        }

        public async Task<Log?> GetLogByIdAsync(Guid id)
        {
            return await _context.Logs.FindAsync(id);
        }

        public async Task<IEnumerable<Log>> GetLogsByOrderIdAsync(Guid orderId)
        {
            return await _context.Logs
                .Where(l => l.OrderId == orderId)
                .OrderByDescending(l => l.SentAt)
                .ToListAsync();
        }

        /// <summary>
        /// Creates a new notification log entry.
        /// Automatically sets the SentAt timestamp to the current UTC time.
        /// </summary>
        public async Task<Log> CreateLogAsync(Log log)
        {
            log.SentAt = DateTime.UtcNow;
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }
    }
}
