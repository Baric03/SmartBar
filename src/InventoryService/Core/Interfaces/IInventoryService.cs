using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryService.Models;

namespace InventoryService.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<Stock>> GetAllStockAsync();
        Task<Stock?> GetStockByIdAsync(Guid id);
        Task<Stock?> GetStockByIngredientAsync(string ingredient);
        Task<Stock> CreateStockAsync(Stock stock);
        Task UpdateStockAsync(Stock stock);
        Task DeleteStockAsync(Guid id);
        Task<bool> HasEnoughStockAsync(string ingredient, int requiredQuantity);
    }
}
