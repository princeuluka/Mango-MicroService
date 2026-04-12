using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMessageBus _messageBus;
        private readonly string _checkoutQueue;
        private readonly string _emailQueue;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQConsumer(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, IMessageBus messageBus)
        {
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
            _messageBus = messageBus;
            _checkoutQueue = _configuration.GetValue<string>("TopicAndQueueNames:CheckoutQueue") ?? "checkoutqueue";
            _emailQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue") ?? "emailshoppingcart";
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

            _channel.QueueDeclare(queue: _checkoutQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var cartDto = JsonConvert.DeserializeObject<CartDto>(json);

                    await HandleCheckoutMessage(cartDto);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _checkoutQueue, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task HandleCheckoutMessage(CartDto cartDto)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            OrderHeader orderHeader = new()
            {
                UserId = cartDto.CartHeader.UserId,
                CouponCode = cartDto.CartHeader.CouponCode,
                Discount = cartDto.CartHeader.Discount,
                OrderTotal = cartDto.CartHeader.CartTotal,
                FirstName = cartDto.CartHeader.FirstName,
                LastName = cartDto.CartHeader.LastName,
                PickupDateTime = DateTime.Parse(cartDto.CartHeader.PickupDateTime ?? DateTime.Now.ToString()),
                Phone = cartDto.CartHeader.Phone,
                Email = cartDto.CartHeader.Email,
                OrderTime = DateTime.Now,
                Status = "Pending"
            };

            await _db.OrderHeaders.AddAsync(orderHeader);
            await _db.SaveChangesAsync();

            foreach (var detail in cartDto.CartDetails)
            {
                OrderDetails orderDetail = new()
                {
                    OrderHeaderId = orderHeader.OrderHeaderId,
                    ProductId = detail.ProductId,
                    ProductName = detail.Product?.Name ?? "",
                    Price = detail.Product?.Price ?? 0,
                    Count = detail.Count
                };
                await _db.OrderDetails.AddAsync(orderDetail);
            }
            await _db.SaveChangesAsync();

            // Publish to email queue for confirmation
            await _messageBus.PublishMessage(cartDto, _emailQueue);
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
