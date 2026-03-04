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
        /// <param name="request">The order request containing table number and items.</param>
        /// <returns>The created order.</returns>
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderRequest request)
        {
            // Items are comma-separated string, e.g., "Espresso, Milk"
            var items = request.Items.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            foreach (var item in items)
            {
                var stockRequest = new CheckStockRequest { Ingredient = item, Quantity = 1 };
                var response = await _inventoryClient.CheckStockAsync(stockRequest);
                
                if (!response.HasEnoughStock)
                {
                    return BadRequest($"Not enough stock for: {item}");
                }
            }

            // Second pass: Deduct stock since we verified we have enough
            foreach (var item in items)
            {
                var stockRequest = new CheckStockRequest { Ingredient = item, Quantity = 1 };
                await _inventoryClient.DeductStockAsync(stockRequest);
            }

            // Map request DTO to Order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                TableNum = request.TableNum,
                Items = request.Items,
                Status = "Pending"
            };

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
