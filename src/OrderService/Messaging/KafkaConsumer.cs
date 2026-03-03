using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    /// the corresponding order's status to "Ready" in the OrderService database.
    /// </summary>
    public class KafkaConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer> logger, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Main execution loop. Listens for "drink-ready-events" topic and
        /// updates the order status to "Ready" when a drink is completed.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
            var groupId = "order-service-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("drink-ready-events");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult?.Message != null)
                        {
                            var drinkEvent = JsonSerializer.Deserialize<DrinkReadyEvent>(consumeResult.Message.Value);
                            if (drinkEvent != null)
                            {
                                await ProcessDrinkReadyEvent(drinkEvent);
                            }
                        }
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError("Consume error: {Reason}", e.Error.Reason);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }

        private async Task ProcessDrinkReadyEvent(DrinkReadyEvent drinkEvent)
        {
            _logger.LogInformation(
                "OrderService received drink-ready event: {DrinkName} for order {OrderId}",
                drinkEvent.DrinkName, drinkEvent.OrderId);

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var order = await orderService.GetOrderByIdAsync(drinkEvent.OrderId);
            if (order != null)
            {
                // Use 3-status logic: Pending → In Process → Ready
                order.Status = drinkEvent.AllDrinksReady ? "Ready" : "In Process";
                await orderService.UpdateOrderAsync(order);
                _logger.LogInformation("Order {OrderId} status updated to {Status}.",
                    drinkEvent.OrderId, order.Status);
            }
            else
            {
                _logger.LogWarning("Order {OrderId} not found when processing drink-ready event.", drinkEvent.OrderId);
            }
        }
    }
}
