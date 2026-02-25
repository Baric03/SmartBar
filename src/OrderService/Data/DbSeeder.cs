using System;
using System.Linq;
using OrderService.Models;

namespace OrderService.Data
{
    public static class DbSeeder
    {
        public static void Seed(OrderDbContext context)
        {
            context.Database.EnsureCreated(); // Or run Migrations here

            if (!context.Orders.Any())
            {
                context.Orders.AddRange(
                    new Order
                    {
                        Id = Guid.NewGuid(),
                        TableNum = 1,
                        Items = "Coffee, Croissant",
                        Status = "Pending"
                    },
                    new Order
                    {
                        Id = Guid.NewGuid(),
                        TableNum = 2,
                        Items = "Tea, Sandwich",
                        Status = "Ready"
                    }
                );
                
                context.SaveChanges();
            }
        }
    }
}
