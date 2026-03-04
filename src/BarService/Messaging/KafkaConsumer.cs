using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
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
        private readonly ILogger<KafkaConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _bootstrapServers;

        /// <summary>
        /// Reactive subject that bridges Kafka consumption into the Rx world.
        /// Each successfully deserialized event is pushed here for downstream processing.
        /// </summary>
        private readonly Subject<OrderCreatedEvent> _orderEventSubject = new();

        public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
        }

        /// <summary>
        /// Main execution loop. Kafka messages are consumed on a background thread and
        /// pushed into the reactive pipeline via <see cref="_orderEventSubject"/>.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = "bar-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var subscription = SetupReactivePipeline();

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("order-events");

            try
            {
                await ConsumeLoop(consumer, stoppingToken);
            }
            finally
            {
                // Signal the reactive pipeline that no more events will arrive
                _orderEventSubject.OnCompleted();
                subscription.Dispose();
                _orderEventSubject.Dispose();
            }
        }

        /// <summary>
        /// Sets up the reactive pipeline that buffers and processes order events.
        /// </summary>
        private IDisposable SetupReactivePipeline()
        {
            return _orderEventSubject
                .Buffer(TimeSpan.FromSeconds(3))
                .Where(batch => batch.Count > 0)
                .Subscribe(
                    batch =>
                    {
                        var batchCount = batch.Count;
                        _logger.LogInformation(
                            "[Rx] Processing batch of {Count} order event(s) received in the last 3s window.",
                            batchCount);

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
                            "[Rx] Batch complete — {Count} order(s) processed successfully.", batchCount);
                    },
                    error => _logger.LogError(error, "[Rx] Fatal error in reactive pipeline."),
                    () => _logger.LogInformation("[Rx] Reactive order pipeline completed.")
                );
        }

        /// <summary>
        /// Main consume loop that reads messages from Kafka and pushes them to the reactive pipeline.
        /// </summary>
        private async Task ConsumeLoop(IConsumer<string, string> consumer, CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeNext(consumer, stoppingToken);
                    await Task.CompletedTask;
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "BarService Kafka consumer shutting down gracefully.");
                consumer.Close();
            }
        }

        /// <summary>
        /// Consumes a single message from Kafka and pushes it into the reactive stream.
        /// </summary>
        private void ConsumeNext(IConsumer<string, string> consumer, CancellationToken stoppingToken)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);

                if (consumeResult?.Message != null)
                {
                    var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);
                    if (orderEvent != null)
                    {
                        _orderEventSubject.OnNext(orderEvent);
                    }
                }
            }
            catch (ConsumeException e)
            {
                _logger.LogError(e, "Consume error: {Reason}", e.Error.Reason);
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
