using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BarService.Messaging
{
    public interface IKafkaProducer
    {
        Task ProduceAsync<T>(string topic, string key, T message);
    }

    public class KafkaProducer : IKafkaProducer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly IProducer<string, string> _producer;

        public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            var bootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:29092";
            
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task ProduceAsync<T>(string topic, string key, T message)
        {
            try
            {
                var value = JsonSerializer.Serialize(message);
                var dr = await _producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = value });
                _logger.LogInformation($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
            }
            catch (ProduceException<string, string> e)
            {
                _logger.LogError($"Delivery failed: {e.Error.Reason}");
            }
        }
    }
}
