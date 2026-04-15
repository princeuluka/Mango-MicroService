using Mango.Service.EmailAPI.Services;
using Mango.Services.EmailAPI.Models.Dto;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Service.EmailAPI.Messaging
{
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IConfiguration? _configuration;
        private readonly EmailService _emailService;
        private readonly string _emailCartQueue;
        private readonly string _orderCreatedQueue;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQConsumer(IConfiguration? configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _emailCartQueue = _configuration?.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue") ?? "emailshoppingcart";
            _orderCreatedQueue = _configuration?.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic") ?? "ordercreated";
        }

        public Task Start()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration?.GetValue<string>("MessageBus:Host") ?? "localhost",
                Port = int.TryParse(_configuration?.GetValue<string>("MessageBus:Port"), out var p) ? p : 5672,
                UserName = _configuration?.GetValue<string>("MessageBus:UserName") ?? "guest",
                Password = _configuration?.GetValue<string>("MessageBus:Password") ?? "guest",
                DispatchConsumersAsync = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: _emailCartQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueDeclare(queue: _orderCreatedQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var cartConsumer = new AsyncEventingBasicConsumer(_channel);
            cartConsumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var cartDto = JsonConvert.DeserializeObject<CartDto>(json);
                    await _emailService.EmailCartAndLog(cartDto);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _emailCartQueue, autoAck: false, consumer: cartConsumer);

            var orderConsumer = new AsyncEventingBasicConsumer(_channel);
            orderConsumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(json);
                    await _emailService.EmailOrderPlaced(orderHeaderDto);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _orderCreatedQueue, autoAck: false, consumer: orderConsumer);
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
