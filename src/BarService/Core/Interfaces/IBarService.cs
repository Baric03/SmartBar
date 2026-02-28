using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarService.Models;

namespace BarService.Core.Interfaces
{
    public interface IBarService
    {
        Task<IEnumerable<DrinkTask>> GetAllDrinkTasksAsync();
        Task<DrinkTask?> GetDrinkTaskByIdAsync(Guid id);
        Task<DrinkTask> CreateDrinkTaskAsync(DrinkTask drinkTask);
        Task UpdateDrinkTaskAsync(DrinkTask drinkTask);
        Task DeleteDrinkTaskAsync(Guid id);
    }
}
