using Mango.MessageBus;
using Mango.Service.EmailAPI.Services;
using Mango.Services.EmailAPI.Models.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Service.EmailAPI.Messaging
{
    public class RabbitMQConsumer : IMessageConsumer
    {
        private readonly IRabbitMQConnection _connection;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IModel _channel;
        private string _queueName;

        public RabbitMQConsumer(
            IRabbitMQConnection connection,
            IConfiguration configuration,
            EmailService emailService,
            ILogger<RabbitMQConsumer> logger)
        {
            _connection = connection;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _queueName = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue") ?? "emailshoppingcart";
        }

        public async Task Start()
        {
            _logger.LogInformation("Starting RabbitMQ consumer for queue: {QueueName}", _queueName);
            
            _channel = _connection.CreateChannel();
            
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Received message from queue {QueueName}", _queueName);

                try
                {
                    CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(message);
                    await _emailService.EmailCartAndLog(cartDto);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Message processed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            consumer.Shutdown += (model, ea) =>
            {
                _logger.LogWarning("Consumer shutdown: {ReplyText}", ea.ReplyText);
            };

            consumer.Registered += (model, ea) =>
            {
                _logger.LogInformation("Consumer registered");
            };

            consumer.Unregistered += (model, ea) =>
            {
                _logger.LogWarning("Consumer unregistered");
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("RabbitMQ consumer started successfully");

            await Task.CompletedTask;
        }

        public async Task Stop()
        {
            _logger.LogInformation("Stopping RabbitMQ consumer");
            
            _channel?.Close();
            _channel?.Dispose();
            
            _logger.LogInformation("RabbitMQ consumer stopped");
            
            await Task.CompletedTask;
        }
    }
}
