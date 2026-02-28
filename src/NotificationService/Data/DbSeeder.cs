using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data
{
    public static class DbSeeder
    {
        public static void Seed(NotificationDbContext context)
        {
            context.Database.Migrate();

            if (!context.Logs.Any())
            {
                var orderId1 = Guid.NewGuid();
                var orderId2 = Guid.NewGuid();

                context.Logs.AddRange(
                    new Log { Id = Guid.NewGuid(), OrderId = orderId1, Message = "Order received.", SentAt = DateTime.UtcNow.AddMinutes(-10) },
                    new Log { Id = Guid.NewGuid(), OrderId = orderId1, Message = "Drink Double Espresso is ready.", SentAt = DateTime.UtcNow.AddMinutes(-5) },
                    new Log { Id = Guid.NewGuid(), OrderId = orderId2, Message = "Order received.", SentAt = DateTime.UtcNow.AddMinutes(-2) }
                );

                context.SaveChanges();
            }
        }
    }
}
