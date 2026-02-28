using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InventoryService.Core.Interfaces;
using InventoryService.Data;
using InventoryService.Models;

namespace InventoryService.Core.Services
{
    public class InventoryManagementService : IInventoryService
    {
        private readonly InventoryDbContext _context;

        public InventoryManagementService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Stock>> GetAllStockAsync()
        {
            return await _context.Stocks.ToListAsync();
        }

        public async Task<Stock?> GetStockByIdAsync(Guid id)
        {
            return await _context.Stocks.FindAsync(id);
        }

        public async Task<Stock?> GetStockByIngredientAsync(string ingredient)
        {
            return await _context.Stocks.FirstOrDefaultAsync(s => s.Ingredient == ingredient);
        }

        public async Task<Stock> CreateStockAsync(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();
            return stock;
        }

        public async Task UpdateStockAsync(Stock stock)
        {
            _context.Entry(stock).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStockAsync(Guid id)
        {
            var stock = await _context.Stocks.FindAsync(id);
            if (stock != null)
            {
                _context.Stocks.Remove(stock);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasEnoughStockAsync(string ingredient, int requiredQuantity)
        {
            var stock = await GetStockByIngredientAsync(ingredient);
            if (stock == null)
            {
                return false;
            }
            return stock.Quantity >= requiredQuantity;
        }

        public async Task<bool> DeductStockAsync(string ingredient, int amount)
        {
            var stock = await GetStockByIngredientAsync(ingredient);
            if (stock == null || stock.Quantity < amount)
            {
                return false;
            }

            stock.Quantity -= amount;
            await UpdateStockAsync(stock);
            
            return true;
        }
    }
}
