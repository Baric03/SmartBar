using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrderService.Controllers;
using OrderService.Core.Interfaces;
using OrderService.Models;
using Xunit;

namespace OrderService.UnitTests
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            _controller = new OrdersController(_mockOrderService.Object);
        }

        [Fact]
        public async Task GetOrders_ShouldReturnOkResult_WithListOfOrders()
        {
            // Arrange
            var mockOrders = new List<Order>
            {
                new Order { Id = Guid.NewGuid(), TableNum = 1, Items = "Coffee", Status = "Pending" },
                new Order { Id = Guid.NewGuid(), TableNum = 2, Items = "Tea", Status = "Ready" }
            };
            
            _mockOrderService.Setup(s => s.GetAllOrdersAsync()).ReturnsAsync(mockOrders);

            // Act
            var result = await _controller.GetOrders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);
            Assert.Equal(2, ((List<Order>)returnedOrders).Count);
        }

        [Fact]
        public async Task GetOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(orderId))
                             .ReturnsAsync((Order?)null); // Returning null explicitly

            // Act
            var result = await _controller.GetOrder(orderId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetOrder_ShouldReturnOkResult_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var expectedOrder = new Order { Id = orderId, TableNum = 3, Items = "Cake", Status = "Pending" };
            
            _mockOrderService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync(expectedOrder);

            // Act
            var result = await _controller.GetOrder(orderId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrder = Assert.IsType<Order>(okResult.Value);
            Assert.Equal(orderId, returnedOrder.Id);
            Assert.Equal("Cake", returnedOrder.Items);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnCreatedAtAction_WithCreatedOrder()
        {
            // Arrange
            var newOrder = new Order { TableNum = 4, Items = "Juice", Status = "Pending" };
            var createdOrder = new Order { Id = Guid.NewGuid(), TableNum = 4, Items = "Juice", Status = "Pending" };
            
            _mockOrderService.Setup(s => s.CreateOrderAsync(newOrder)).ReturnsAsync(createdOrder);

            // Act
            var result = await _controller.CreateOrder(newOrder);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(OrdersController.GetOrder), createdAtActionResult.ActionName);
            Assert.IsType<Order>(createdAtActionResult.Value);
            
            var returnedOrder = (Order)createdAtActionResult.Value;
            Assert.Equal(createdOrder.Id, returnedOrder.Id);
            Assert.Equal(createdOrder.Items, returnedOrder.Items);
        }
    }
}
