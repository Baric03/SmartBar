using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using NotificationService.Controllers;
using NotificationService.Core.Interfaces;
using NotificationService.Models;

namespace NotificationService.UnitTests
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _mockService;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _mockService = new Mock<INotificationService>();
            _controller = new NotificationController(_mockService.Object);
        }

        [Fact]
        public async Task GetAllLogs_ReturnsOkResult_WithListOfLogs()
        {
            // Arrange
            var mockLogs = new List<Log>
            {
                new Log { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Message = "Test 1" },
                new Log { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Message = "Test 2" }
            };
            _mockService.Setup(s => s.GetAllLogsAsync()).ReturnsAsync(mockLogs);

            // Act
            var result = await _controller.GetAllLogs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnLogs = Assert.IsAssignableFrom<IEnumerable<Log>>(okResult.Value);
            Assert.Equal(2, returnLogs.Count());
        }

        [Fact]
        public async Task GetLogById_ExistingId_ReturnsOkResult_WithLog()
        {
            // Arrange
            var logId = Guid.NewGuid();
            var mockLog = new Log { Id = logId, OrderId = Guid.NewGuid(), Message = "Found you" };
            _mockService.Setup(s => s.GetLogByIdAsync(logId)).ReturnsAsync(mockLog);

            // Act
            var result = await _controller.GetLogById(logId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnLog = Assert.IsType<Log>(okResult.Value);
            Assert.Equal(logId, returnLog.Id);
        }

        [Fact]
        public async Task GetLogById_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            var logId = Guid.NewGuid();
            _mockService.Setup(s => s.GetLogByIdAsync(logId)).ReturnsAsync((Log?)null);

            // Act
            var result = await _controller.GetLogById(logId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetLogsByOrderId_ExistingOrderId_ReturnsOkResult_WithListOfLogs()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var mockLogs = new List<Log>
            {
                new Log { Id = Guid.NewGuid(), OrderId = orderId, Message = "Order received" },
                new Log { Id = Guid.NewGuid(), OrderId = orderId, Message = "Drink ready" }
            };
            _mockService.Setup(s => s.GetLogsByOrderIdAsync(orderId)).ReturnsAsync(mockLogs);

            // Act
            var result = await _controller.GetLogsByOrderId(orderId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnLogs = Assert.IsAssignableFrom<IEnumerable<Log>>(okResult.Value);
            Assert.Equal(2, returnLogs.Count());
        }

        [Fact]
        public async Task CreateLog_ValidLog_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var newLog = new Log { OrderId = Guid.NewGuid(), Message = "New Log" };
            var createdLog = new Log { Id = Guid.NewGuid(), OrderId = newLog.OrderId, Message = newLog.Message };
            
            _mockService.Setup(s => s.CreateLogAsync(newLog)).ReturnsAsync(createdLog);

            // Act
            var result = await _controller.CreateLog(newLog);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnLog = Assert.IsType<Log>(createdAtActionResult.Value);
            Assert.Equal(createdLog.Id, returnLog.Id);
            Assert.Equal("GetLogById", createdAtActionResult.ActionName);
        }
    }
}
