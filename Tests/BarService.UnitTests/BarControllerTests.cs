using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BarService.Controllers;
using BarService.Core.Interfaces;
using BarService.Models;

namespace BarService.UnitTests
{
    public class BarControllerTests
    {
        private readonly Mock<IBarService> _mockService;
        private readonly BarController _controller;

        public BarControllerTests()
        {
            _mockService = new Mock<IBarService>();
            _controller = new BarController(_mockService.Object);
        }

        [Fact]
        public async Task GetAllDrinkTasks_ReturnsOkResult_WithListOfDrinkTasks()
        {
            // Arrange
            var mockTasks = new List<DrinkTask>
            {
                new DrinkTask { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Name = "Double Espresso", IsReady = false },
                new DrinkTask { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Name = "Cappuccino", IsReady = true }
            };
            _mockService.Setup(s => s.GetAllDrinkTasksAsync()).ReturnsAsync(mockTasks);

            // Act
            var result = await _controller.GetAllDrinkTasks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnTasks = Assert.IsAssignableFrom<IEnumerable<DrinkTask>>(okResult.Value);
            Assert.Equal(2, ((List<DrinkTask>)returnTasks).Count);
        }

        [Fact]
        public async Task GetDrinkTaskById_ExistingId_ReturnsOkResult_WithDrinkTask()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var mockTask = new DrinkTask { Id = taskId, OrderId = Guid.NewGuid(), Name = "Americano", IsReady = false };
            _mockService.Setup(s => s.GetDrinkTaskByIdAsync(taskId)).ReturnsAsync(mockTask);

            // Act
            var result = await _controller.GetDrinkTaskById(taskId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnTask = Assert.IsType<DrinkTask>(okResult.Value);
            Assert.Equal(taskId, returnTask.Id);
        }

        [Fact]
        public async Task GetDrinkTaskById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            _mockService.Setup(s => s.GetDrinkTaskByIdAsync(taskId)).ReturnsAsync((DrinkTask?)null);

            // Act
            var result = await _controller.GetDrinkTaskById(taskId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateDrinkTask_ValidTask_ReturnsNoContent()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var drinkTask = new DrinkTask { Id = taskId, OrderId = Guid.NewGuid(), Name = "Latte", IsReady = true };
            _mockService.Setup(s => s.GetDrinkTaskByIdAsync(taskId)).ReturnsAsync(drinkTask);
            _mockService.Setup(s => s.UpdateDrinkTaskAsync(It.IsAny<DrinkTask>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateDrinkTask(taskId, drinkTask);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateDrinkTask_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var drinkTask = new DrinkTask { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Name = "Latte", IsReady = true };

            // Act
            var result = await _controller.UpdateDrinkTask(taskId, drinkTask);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }
    }
}
