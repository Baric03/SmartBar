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

        }
    }
}
