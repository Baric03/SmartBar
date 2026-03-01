using Microsoft.AspNetCore.Mvc;
using OrderService.Core.Interfaces;
using OrderService.Models;
using InventoryService.Protos;

namespace OrderService.Controllers
{
    /// <summary>
    /// Controller for managing bar orders.
    /// Handles order creation, including stock verification via gRPC and event publishing via Kafka.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly InventoryGrpcConfig.InventoryGrpcConfigClient _inventoryClient;
        private readonly OrderService.Messaging.IKafkaProducer _kafkaProducer;

        public OrdersController(IOrderService orderService, InventoryGrpcConfig.InventoryGrpcConfigClient inventoryClient, OrderService.Messaging.IKafkaProducer kafkaProducer)
        {
            _orderService = orderService;
            _inventoryClient = inventoryClient;
            _kafkaProducer = kafkaProducer;
        }

        /// <summary>
        /// Retrieves all orders from the database.
        /// </summary>
        /// <returns>A list of all orders.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        /// <summary>
        /// Retrieves a specific order by its unique identifier.
        /// </summary>
        /// <param name="id">The GUID of the order.</param>
        /// <returns>The requested order or 404 if not found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(Guid id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        /// <summary>
        /// Creates a new order. 
        /// Performs stock verification and deduction via gRPC with InventoryService,
        /// then publishes an OrderCreatedEvent to Kafka.
        /// </summary>
        /// <param name="order">The order details.</param>
        /// <returns>The created order.</returns>
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            // Simple mockup logic: items are comma-separated string, e.g., "Espresso, Milk"
            var items = order.Items.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            foreach (var item in items)
            {
                var request = new CheckStockRequest { Ingredient = item, Quantity = 1 }; // Assuming 1 quantity per item for now
                var response = await _inventoryClient.CheckStockAsync(request);
                
                if (!response.HasEnoughStock)
                {
                    return BadRequest($"Not enough stock for: {item}");
                }
            }

            // Second pass: Deduct stock since we verified we have enough
            foreach (var item in items)
            {
                var request = new CheckStockRequest { Ingredient = item, Quantity = 1 };
                await _inventoryClient.DeductStockAsync(request);
            }

            var createdOrder = await _orderService.CreateOrderAsync(order);

            var orderEvent = new OrderService.Events.OrderCreatedEvent
            {
                OrderId = createdOrder.Id,
                TableNum = createdOrder.TableNum,
                Items = createdOrder.Items
            };

            await _kafkaProducer.ProduceAsync("order-events", orderEvent.OrderId.ToString(), orderEvent);

            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
        }
    }
}
