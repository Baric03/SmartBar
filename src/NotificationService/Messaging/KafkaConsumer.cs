using System.Text.Json;
using NotificationService.Core.Interfaces;
using NotificationService.Events;
using NotificationService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Messaging
{
    /// <summary>
    /// Background worker that consumes multiple event types from Kafka.
    /// It captures both "order-events" and "drink-ready-events" to maintain a unified audit log.
    /// </summary>
    public class KafkaConsumer : BackgroundService
    {
        private static readonly string[] Topics = ["order-events", "drink-ready-events"];

        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _bootstrapServers;

        public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
        }

        /// <summary>
        /// Main execution loop. Subscribes to multiple topics and routes messages to specific handlers based on the topic name.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = "notification-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(Topics);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await ConsumeNext(consumer, stoppingToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "NotificationService Kafka consumer shutting down gracefully.");
                consumer.Close();
            }
        }

        /// <summary>
        /// Consumes a single message from Kafka and routes it to the correct handler.
        /// </summary>
        private async Task ConsumeNext(IConsumer<string, string> consumer, CancellationToken stoppingToken)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult?.Message == null) return;

                if (consumeResult.Topic == "order-events")
                {
                    var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);
                    if (orderEvent != null)
                    {
                        await ProcessNotificationEvent(orderEvent);
                    }
                }
                else if (consumeResult.Topic == "drink-ready-events")
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
                _logger.LogError(e, "Consume error: {Reason}", e.Error.Reason);
            }
        }

        private async Task ProcessNotificationEvent(OrderCreatedEvent orderEvent)
        {
            _logger.LogInformation("NotificationService received order: {OrderId} at table {TableNum}", orderEvent.OrderId, orderEvent.TableNum);

            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var log = new Log
            {
                Id = Guid.NewGuid(),
                OrderId = orderEvent.OrderId,
                Message = $"Order created at table {orderEvent.TableNum} for items: {orderEvent.Items}",
                SentAt = DateTime.Now
            };

            await notificationService.CreateLogAsync(log);
        }

        private async Task ProcessDrinkReadyEvent(DrinkReadyEvent drinkEvent)
        {
            _logger.LogInformation("NotificationService received drink ready: {DrinkName} for order {OrderId}", drinkEvent.DrinkName, drinkEvent.OrderId);

            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var log = new Log
            {
                Id = Guid.NewGuid(),
                OrderId = drinkEvent.OrderId,
                Message = $"Drink {drinkEvent.DrinkName} is ready for order {drinkEvent.OrderId}",
                SentAt = DateTime.Now
            };

            await notificationService.CreateLogAsync(log);
        }
    }
}
