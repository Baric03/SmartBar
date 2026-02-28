using BarService.Models;
using Microsoft.EntityFrameworkCore;

namespace BarService.Data
{
    public class BarDbContext : DbContext
    {
        public BarDbContext(DbContextOptions<BarDbContext> options) : base(options)
        {
        }

        public DbSet<DrinkTask> DrinkTasks { get; set; }
    }
}
