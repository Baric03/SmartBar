using System;
using System.Linq;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data
{
    public static class DbSeeder
    {
        public static void Seed(InventoryDbContext context)
        {
            context.Database.Migrate();

            if (!context.Stocks.Any())
            {
                context.Stocks.AddRange(
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Espresso", Quantity = 100 },
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Milk", Quantity = 200 },
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Water", Quantity = 500 },
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Juice", Quantity = 50 },
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Syrup", Quantity = 25 },
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Tea", Quantity = 300 },
                    new Stock { Id = Guid.NewGuid(), Ingredient = "Hot Chocolate", Quantity = 50 }
                );

                context.SaveChanges();
            }
        }
    }
}
