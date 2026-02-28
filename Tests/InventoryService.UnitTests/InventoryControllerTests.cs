using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using InventoryService.Controllers;
using InventoryService.Core.Interfaces;
using InventoryService.Models;

namespace InventoryService.UnitTests
{
    public class InventoryControllerTests
    {
        private readonly Mock<IInventoryService> _mockService;
        private readonly InventoryController _controller;

        public InventoryControllerTests()
        {
            _mockService = new Mock<IInventoryService>();
            _controller = new InventoryController(_mockService.Object);
        }

        [Fact]
        public async Task GetAllStock_ReturnsOkResult_WithListOfStock()
        {
            // Arrange
            var mockStocks = new List<Stock>
            {
                new Stock { Id = Guid.NewGuid(), Ingredient = "Espresso", Quantity = 100 },
                new Stock { Id = Guid.NewGuid(), Ingredient = "Milk", Quantity = 200 }
            };
            _mockService.Setup(s => s.GetAllStockAsync()).ReturnsAsync(mockStocks);

            // Act
            var result = await _controller.GetAllStock();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnStocks = Assert.IsAssignableFrom<IEnumerable<Stock>>(okResult.Value);
            Assert.Equal(2, ((List<Stock>)returnStocks).Count);
        }

        [Fact]
        public async Task GetStockById_ExistingId_ReturnsOkResult_WithStock()
        {
            // Arrange
            var stockId = Guid.NewGuid();
            var mockStock = new Stock { Id = stockId, Ingredient = "Water", Quantity = 500 };
            _mockService.Setup(s => s.GetStockByIdAsync(stockId)).ReturnsAsync(mockStock);

            // Act
            var result = await _controller.GetStockById(stockId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnStock = Assert.IsType<Stock>(okResult.Value);
            Assert.Equal(stockId, returnStock.Id);
        }

        [Fact]
        public async Task GetStockById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            var stockId = Guid.NewGuid();
            _mockService.Setup(s => s.GetStockByIdAsync(stockId)).ReturnsAsync((Stock?)null);

            // Act
            var result = await _controller.GetStockById(stockId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CheckStock_EnoughStock_ReturnsTrue()
        {
            // Arrange
            string ingredient = "Espresso";
            int quantity = 2;
            _mockService.Setup(s => s.HasEnoughStockAsync(ingredient, quantity)).ReturnsAsync(true);

            // Act
            var result = await _controller.CheckStock(ingredient, quantity);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var hasEnough = Assert.IsType<bool>(okResult.Value);
            Assert.True(hasEnough);
        }
    }
}
