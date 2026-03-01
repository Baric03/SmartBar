using System;
using System.Linq;
using BarService.Models;
using Microsoft.EntityFrameworkCore;

namespace BarService.Data
{
    public static class DbSeeder
    {
        public static void Seed(BarDbContext context)
        {
            context.Database.Migrate();

            /*if (!context.DrinkTasks.Any())
            {
                context.DrinkTasks.AddRange(
                    new DrinkTask { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Name = "Double Espresso", IsReady = false },
                    new DrinkTask { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Name = "Cappuccino", IsReady = false },
                    new DrinkTask { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Name = "Americano", IsReady = true }
                );

                context.SaveChanges();
            }*/
        }
    }
}
