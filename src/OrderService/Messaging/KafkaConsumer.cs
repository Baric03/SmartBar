using System.Text.Json;
using OrderService.Core.Interfaces;
using OrderService.Events;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrderService.Messaging
{
    /// <summary>
    /// Background worker that consumes drink-ready events from Kafka.
    /// When a drink is marked as ready in BarService, this consumer updates
    /// the corresponding order's status using 3-state logic:
    /// Pending → In Process → Ready.
    /// </summary>
    public class KafkaConsumer : BackgroundService
    {
        private const string Topic = "drink-ready-events";
        private const string ConsumerGroup = "order-service-group";

        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _bootstrapServers;

        public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = BuildConsumer();
            consumer.Subscribe(Topic);
            _logger.LogInformation("OrderService Kafka consumer started, listening on topic: {Topic}", Topic);

            await ConsumeLoop(consumer, stoppingToken);
        }

        /// <summary>
        /// Builds a configured Kafka consumer instance using application settings.
        /// </summary>
        private IConsumer<string, string> BuildConsumer()
        {
            return new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = ConsumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            }).Build();
        }

        /// <summary>
        /// Main consume loop that reads messages and delegates processing.
        /// Continues until cancellation is requested.
        /// </summary>
        private async Task ConsumeLoop(IConsumer<string, string> consumer, CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await ConsumeNext(consumer, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OrderService Kafka consumer shutting down gracefully.");
                consumer.Close();
            }
        }

        /// <summary>
        /// Consumes a single message from Kafka and processes it.
        /// </summary>
        private async Task ConsumeNext(IConsumer<string, string> consumer, CancellationToken stoppingToken)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value == null) return;

                var drinkEvent = JsonSerializer.Deserialize<DrinkReadyEvent>(result.Message.Value);
                if (drinkEvent != null)
                {
                    await UpdateOrderStatus(drinkEvent);
                }
            }
            catch (ConsumeException e)
            {
                _logger.LogError(e, "Kafka consume error: {Reason}", e.Error.Reason);
            }
        }

        /// <summary>
        /// Updates the order status based on the drink-ready event.
        /// If all drinks are ready, status becomes "Ready"; otherwise "In Process".
        /// </summary>
        private async Task UpdateOrderStatus(DrinkReadyEvent drinkEvent)
        {
            _logger.LogInformation(
                "Received drink-ready event: {DrinkName} for order {OrderId} (AllReady: {AllReady})",
                drinkEvent.DrinkName, drinkEvent.OrderId, drinkEvent.AllDrinksReady);

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var order = await orderService.GetOrderByIdAsync(drinkEvent.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for drink-ready event.", drinkEvent.OrderId);
                return;
            }

            order.Status = drinkEvent.AllDrinksReady ? "Ready" : "In Process";
            await orderService.UpdateOrderAsync(order);
            _logger.LogInformation("Order {OrderId} status updated to {Status}.", drinkEvent.OrderId, order.Status);
        }
    }
}
