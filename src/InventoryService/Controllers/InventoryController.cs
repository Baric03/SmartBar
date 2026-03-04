using Microsoft.AspNetCore.Mvc;
using InventoryService.Core.Interfaces;
using InventoryService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stock>>> GetAllStock()
        {
            var stocks = await _inventoryService.GetAllStockAsync();
            return Ok(stocks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Stock>> GetStockById(Guid id)
        {
            var stock = await _inventoryService.GetStockByIdAsync(id);

            if (stock == null)
            {
                return NotFound();
            }

            return Ok(stock);
        }

        [HttpGet("ingredient/{ingredient}")]
        public async Task<ActionResult<Stock>> GetStockByIngredient(string ingredient)
        {
            var stock = await _inventoryService.GetStockByIngredientAsync(ingredient);

            if (stock == null)
            {
                return NotFound();
            }

            return Ok(stock);
        }

        [HttpPost]
        public async Task<ActionResult<Stock>> CreateStock(Stock stock)
        {
            var createdStock = await _inventoryService.CreateStockAsync(stock);

            return CreatedAtAction(nameof(GetStockById), new { id = createdStock.Id }, createdStock);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStock(Guid id, Stock stock)
        {
            if (id != stock.Id)
            {
                return BadRequest();
            }

            var existingStock = await _inventoryService.GetStockByIdAsync(id);
            if (existingStock == null)
            {
                return NotFound();
            }

            await _inventoryService.UpdateStockAsync(stock);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStock(Guid id)
        {
            var existingStock = await _inventoryService.GetStockByIdAsync(id);
            if (existingStock == null)
            {
                return NotFound();
            }

            await _inventoryService.DeleteStockAsync(id);

            return NoContent();
        }

        [HttpGet("check/{ingredient}/{quantity}")]
        public async Task<ActionResult<bool>> CheckStock(string ingredient, int quantity)
        {
            var hasEnough = await _inventoryService.HasEnoughStockAsync(ingredient, quantity);
            return Ok(hasEnough);
        }
    }
}
