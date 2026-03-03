using System;
using System.Linq;
using OrderService.Models;

namespace OrderService.Data
{
    public static class DbSeeder
    {
        public static void Seed(OrderDbContext context)
        {
            context.Database.EnsureCreated();
        }
    }
}
