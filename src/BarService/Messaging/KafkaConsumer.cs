using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BarService.Core.Interfaces;
using BarService.Events;
using BarService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BarService.Messaging
{
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
            var groupId = "bar-service-group";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("order-events");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult != null && consumeResult.Message != null)
                        {
                            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);
                            if (orderEvent != null)
                            {
                                await ProcessOrderEvent(orderEvent);
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

        private async Task ProcessOrderEvent(OrderCreatedEvent orderEvent)
        {
            _logger.LogInformation($"BarService received order: {orderEvent.OrderId} at table {orderEvent.TableNum}");
            
            using var scope = _serviceProvider.CreateScope();
            var barService = scope.ServiceProvider.GetRequiredService<IBarService>();
            
            // split items by comma and create drink tasks for each
            var items = orderEvent.Items.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            foreach (var item in items)
            {
                var drinkTask = new DrinkTask
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderEvent.OrderId,
                    Name = item,
                    IsReady = false
                };
                
                await barService.CreateDrinkTaskAsync(drinkTask);
            }
        }
    }
}
