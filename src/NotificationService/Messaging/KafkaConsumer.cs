using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        /// Main execution loop. Subscribes to multiple topics and routes messages to specific handlers based on the topic name.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
            var groupId = "notification-service-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(new[] { "order-events", "drink-ready-events" });

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult != null && consumeResult.Message != null)
                        {
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
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError($"Consume error: {e.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                consumer.Close();
            }
        }

        private async Task ProcessNotificationEvent(OrderCreatedEvent orderEvent)
        {
            _logger.LogInformation($"NotificationService received order: {orderEvent.OrderId} at table {orderEvent.TableNum}");
            
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
            _logger.LogInformation($"NotificationService received drink ready: {drinkEvent.DrinkName} for order {drinkEvent.OrderId}");
            
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
