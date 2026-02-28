using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotificationService.Models;

namespace NotificationService.Core.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<Log>> GetAllLogsAsync();
        Task<Log?> GetLogByIdAsync(Guid id);
        Task<IEnumerable<Log>> GetLogsByOrderIdAsync(Guid orderId);
        Task<Log> CreateLogAsync(Log log);
    }
}
