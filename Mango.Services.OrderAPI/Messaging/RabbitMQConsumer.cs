using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IMessageBus _messageBus;
        private readonly string _checkoutQueue;
        private readonly string _emailOrderQueue;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQConsumer(IConfiguration configuration, AppDbContext db, IMapper mapper, IMessageBus messageBus)
        {
            _configuration = configuration;
            _db = db;
            _mapper = mapper;
            _messageBus = messageBus;
            _checkoutQueue = _configuration.GetValue<string>("TopicAndQueueNames:CheckoutQueue") ?? "checkoutqueue";
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
                    await CreateOrder(cartDto);
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

        private async Task CreateOrder(CartDto cartDto)
        {
            var orderHeader = new OrderHeader
            {
                UserId = cartDto.CartHeader.UserId,
                CouponCode = cartDto.CartHeader.CouponCode,
                Discount = cartDto.CartHeader.Discount,
                OrderTotal = cartDto.CartHeader.CartTotal,
                OrderStatus = "Pending",
                PaymentStatus = "Pending",
                CreatedAt = DateTime.Now,
                FirstName = cartDto.CartHeader.FirstName,
                LastName = cartDto.CartHeader.LastName,
                Phone = cartDto.CartHeader.Phone,
                Email = cartDto.CartHeader.Email
            };

            var orderDetailsList = new List<OrderDetails>();
            foreach (var item in cartDto.CartDetails)
            {
                var orderDetails = new OrderDetails
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Price = item.Product.Price,
                    Count = item.Count
                };
                orderDetailsList.Add(orderDetails);
            }
            orderHeader.OrderDetails = orderDetailsList;

            _db.OrderHeaders.Add(orderHeader);
            await _db.SaveChangesAsync();

            // Publish order confirmation to email queue
            var orderDto = _mapper.Map<OrderDto>(orderHeader);
            await _messageBus.PublishMessage(orderDto, _emailOrderQueue);
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
