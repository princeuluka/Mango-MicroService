using Mango.Service.EmailAPI.Services;
using Mango.Services.EmailAPI.Models.Dto;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Service.EmailAPI.Messaging
{
    public class RabbitMQConsumer : IMessageConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName;

        public RabbitMQConsumer(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            
            var factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            var port = _configuration.GetValue<int?>("RabbitMQ:Port");
            if (port.HasValue)
            {
                factory.Port = port.Value;
            }

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _queueName = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");
            
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public async Task Start()
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(message);
                
                try
                {
                    await _emailService.EmailCartAndLog(cartDto);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer
            );

            await Task.CompletedTask;
        }

        public async Task Stop()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            
            await Task.CompletedTask;
        }
    }
}
