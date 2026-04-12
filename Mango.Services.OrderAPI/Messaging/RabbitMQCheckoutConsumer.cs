using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Service.IService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQCheckoutConsumer : IRabbitMQCheckoutConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _checkoutQueue;
        private readonly string _orderConfirmedQueue;
        private readonly string _emailOrderQueue;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQCheckoutConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _checkoutQueue = _configuration.GetValue<string>("TopicAndQueueNames:CheckoutQueue") ?? "checkoutqueue";
            _orderConfirmedQueue = _configuration.GetValue<string>("TopicAndQueueNames:OrderConfirmedQueue") ?? "orderconfirmedqueue";
            _emailOrderQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailOrderQueue") ?? "emailorderqueue";
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
                    await ProcessCheckout(cartDto);
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

        private async Task ProcessCheckout(CartDto cartDto)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            var orderHeader = mapper.Map<OrderHeader>(cartDto.CartHeader);
            orderHeader.OrderTime = DateTime.UtcNow;
            orderHeader.OrderStatus = OrderStatus.Pending;
            orderHeader.PaymentStatus = "Pending";

            // Fetch products to verify prices
            var products = await productService.GetProducts();
            var productDict = products.ToDictionary(p => p.ProductId);

            var orderDetailsList = new List<OrderDetails>();
            foreach (var cartDetail in cartDto.CartDetails)
            {
                // Verify price from ProductAPI
                if (productDict.TryGetValue(cartDetail.ProductId, out var product))
                {
                    var orderDetail = new OrderDetails
                    {
                        ProductId = cartDetail.ProductId,
                        ProductName = product.Name,
                        Price = product.Price,
                        Count = cartDetail.Count
                    };
                    orderDetailsList.Add(orderDetail);
                }
            }

            orderHeader.OrderDetails = orderDetailsList;

            // Recalculate order total based on verified prices
            orderHeader.OrderTotal = orderDetailsList.Sum(od => od.Price * od.Count);
            orderHeader.Discount = cartDto.CartHeader.Discount;
            orderHeader.OrderTotal -= orderHeader.Discount;

            db.OrderHeaders.Add(orderHeader);
            await db.SaveChangesAsync();

            // Publish order confirmed event for ShoppingCartAPI to clear cart
            var orderHeaderDto = mapper.Map<OrderHeaderDto>(orderHeader);
            await messageBus.PublishMessage(orderHeaderDto, _orderConfirmedQueue);

            // Publish to email queue for order confirmation email
            await messageBus.PublishMessage(orderHeaderDto, _emailOrderQueue);
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
