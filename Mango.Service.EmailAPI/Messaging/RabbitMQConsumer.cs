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
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly string _emailCartQueue;
        private readonly string _checkoutQueue;
        private readonly string _orderCreatedQueue;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQConsumer(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue") ?? "emailshoppingcart";
            _checkoutQueue = _configuration.GetValue<string>("TopicAndQueueNames:CheckoutQueue") ?? "checkoutqueue";
            _orderCreatedQueue = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic") ?? "ordercreated";
        }

        public Task Start()
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration.GetValue<string>("MessageBus:Host") ?? "localhost",
                Port = int.TryParse(_configuration.GetValue<string>("MessageBus:Port"), out var p) ? p : 5672,
                UserName = _configuration.GetValue<string>("MessageBus:UserName") ?? "guest",
                Password = _configuration.GetValue<string>("MessageBus:Password") ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            RegisterConsumer(_emailCartQueue, HandleCartMessage);
            RegisterConsumer(_checkoutQueue, HandleCheckoutMessage);
            RegisterConsumer(_orderCreatedQueue, HandleOrderCreatedMessage);

            return Task.CompletedTask;
        }

        private void RegisterConsumer(string queueName, Func<string, Task> messageHandler)
        {
            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    await messageHandler(json);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        private async Task HandleCartMessage(string json)
        {
            var cartDto = JsonConvert.DeserializeObject<CartDto>(json);
            await _emailService.EmailCartAndLog(cartDto);
        }

        private async Task HandleCheckoutMessage(string json)
        {
            var checkoutHeader = JsonConvert.DeserializeObject<CheckoutHeaderDto>(json);
            await _emailService.EmailCheckoutAndLog(checkoutHeader);
        }

        private async Task HandleOrderCreatedMessage(string json)
        {
            var orderHeader = JsonConvert.DeserializeObject<OrderHeaderDto>(json);
            await _emailService.EmailOrderCreatedAndLog(orderHeader);
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
