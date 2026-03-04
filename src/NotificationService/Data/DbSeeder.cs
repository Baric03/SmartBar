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

        }
    }
}
