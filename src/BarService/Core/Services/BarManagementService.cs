using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BarService.Core.Interfaces;
using BarService.Data;
using BarService.Models;

namespace BarService.Core.Services
{
    /// <summary>
    /// Service for managing drink tasks in the bar.
    /// Tracks items that need preparation and their current status.
    /// </summary>
    public class BarManagementService : IBarService
    {
        private readonly BarDbContext _context;

        public BarManagementService(BarDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DrinkTask>> GetAllDrinkTasksAsync()
        {
            return await _context.DrinkTasks.ToListAsync();
        }

        public async Task<IEnumerable<DrinkTask>> GetDrinkTasksByOrderIdAsync(Guid orderId)
        {
            return await _context.DrinkTasks.Where(t => t.OrderId == orderId).ToListAsync();
        }

        public async Task<DrinkTask?> GetDrinkTaskByIdAsync(Guid id)
        {
            return await _context.DrinkTasks.FindAsync(id);
        }

        public async Task<DrinkTask> CreateDrinkTaskAsync(DrinkTask drinkTask)
        {
            _context.DrinkTasks.Add(drinkTask);
            await _context.SaveChangesAsync();
            return drinkTask;
        }

        /// <summary>
        /// Updates an existing drink task (e.g., setting it as ready).
        /// </summary>
        public async Task UpdateDrinkTaskAsync(DrinkTask drinkTask)
        {
            _context.Entry(drinkTask).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDrinkTaskAsync(Guid id)
        {
            var drinkTask = await _context.DrinkTasks.FindAsync(id);
            if (drinkTask != null)
            {
                _context.DrinkTasks.Remove(drinkTask);
                await _context.SaveChangesAsync();
            }
        }
    }
}
