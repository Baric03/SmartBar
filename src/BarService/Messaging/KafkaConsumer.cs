using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    /// <summary>
    /// Background worker that consumes order events from Kafka and processes them
    /// through a System.Reactive (Rx.NET) pipeline. Raw Kafka messages are pushed into
    /// a reactive Subject, then processed via observable operators for buffering,
    /// throughput logging, and resilient error handling.
    /// </summary>
    public class KafkaConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Reactive subject that bridges Kafka consumption into the Rx world.
        /// Each successfully deserialized event is pushed here for downstream processing.
        /// </summary>
        private readonly Subject<OrderCreatedEvent> _orderEventSubject = new();

        public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer> logger, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Main execution loop. Kafka messages are consumed on a background thread and
        /// pushed into the reactive pipeline via <see cref="_orderEventSubject"/>.
        /// </summary>
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

            // --- Reactive Pipeline Setup ---
            // Buffer incoming order events in 3-second windows for batch logging,
            // then process each event individually with error resilience.
            var subscription = _orderEventSubject
                .Buffer(TimeSpan.FromSeconds(3))
                .Where(batch => batch.Count > 0)
                .Subscribe(
                    batch =>
                    {
                        _logger.LogInformation(
                            "[Rx] Processing batch of {Count} order event(s) received in the last 3s window.",
                            batch.Count);

                        foreach (var orderEvent in batch)
                        {
                            try
                            {
                                ProcessOrderEvent(orderEvent).GetAwaiter().GetResult();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,
                                    "[Rx] Error processing order {OrderId}. Continuing with next event.",
                                    orderEvent.OrderId);
                            }
                        }

                        _logger.LogInformation(
                            "[Rx] Batch complete — {Count} order(s) processed successfully.", batch.Count);
                    },
                    error => _logger.LogError(error, "[Rx] Fatal error in reactive pipeline."),
                    () => _logger.LogInformation("[Rx] Reactive order pipeline completed.")
                );

            // --- Kafka Consumption Loop ---
            // Raw Kafka messages are consumed here and pushed into the reactive subject.
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("order-events");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult?.Message != null)
                        {
                            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);
                            if (orderEvent != null)
                            {
                                // Push event into the reactive stream instead of processing directly
                                _orderEventSubject.OnNext(orderEvent);
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
            finally
            {
                // Signal the reactive pipeline that no more events will arrive
                _orderEventSubject.OnCompleted();
                subscription.Dispose();
                _orderEventSubject.Dispose();
            }
        }

        private async Task ProcessOrderEvent(OrderCreatedEvent orderEvent)
        {
            _logger.LogInformation("BarService received order: {OrderId} at table {TableNum}",
                orderEvent.OrderId, orderEvent.TableNum);

            using var scope = _serviceProvider.CreateScope();
            var barService = scope.ServiceProvider.GetRequiredService<IBarService>();

            // Split items by comma and create drink tasks for each
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
