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
        private readonly string _emailOrderQueue;
        private IConnection? _connection;
        private IModel? _channelCart;
        private IModel? _channelOrder;

        public RabbitMQConsumer(IConfiguration? configuration, EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _emailCartQueue = _configuration?.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue") ?? "emailshoppingcart";
            _emailOrderQueue = _configuration?.GetValue<string>("TopicAndQueueNames:EmailOrderQueue") ?? "emailorderqueue";
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

            // Setup cart email consumer
            _channelCart = _connection.CreateModel();
            _channelCart.QueueDeclare(queue: _emailCartQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var cartConsumer = new AsyncEventingBasicConsumer(_channelCart);
            cartConsumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var cartDto = JsonConvert.DeserializeObject<CartDto>(json);
                    await _emailService.EmailCartAndLog(cartDto);
                    _channelCart.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channelCart.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };
            _channelCart.BasicConsume(queue: _emailCartQueue, autoAck: false, consumer: cartConsumer);

            // Setup order email consumer
            _channelOrder = _connection.CreateModel();
            _channelOrder.QueueDeclare(queue: _emailOrderQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var orderConsumer = new AsyncEventingBasicConsumer(_channelOrder);
            orderConsumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(json);
                    await _emailService.EmailOrderConfirmationAndLog(orderHeaderDto);
                    _channelOrder.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channelOrder.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };
            _channelOrder.BasicConsume(queue: _emailOrderQueue, autoAck: false, consumer: orderConsumer);

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _channelCart?.Close();
            _channelOrder?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
