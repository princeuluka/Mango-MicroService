using Mango.MessageBus;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;
using Mango.Services.OrderAPI.Utility;
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
    public class RabbitMQCheckoutConsumer : IRabbitMQConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<AppDbContext> _dbOptions;
        private readonly IMessageBus _messageBus;
        private readonly string _checkoutQueue;
        private readonly string _orderCreatedTopic;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQCheckoutConsumer(IConfiguration configuration, DbContextOptions<AppDbContext> dbOptions, IMessageBus messageBus)
        {
            _configuration = configuration;
            _dbOptions = dbOptions;
            _messageBus = messageBus;
            _checkoutQueue = _configuration.GetValue<string>("TopicAndQueueNames:CheckoutQueue") ?? "checkoutqueue";
            _orderCreatedTopic = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic") ?? "ordercreated";
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
                    var checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(json);

                    await ProcessCheckout(checkoutHeaderDto);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing checkout: {ex.Message}");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _checkoutQueue, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessCheckout(CheckoutHeaderDto? checkoutHeaderDto)
        {
            if (checkoutHeaderDto == null) return;

            using var db = new AppDbContext(_dbOptions);

            OrderHeader orderHeader = new()
            {
                UserId = checkoutHeaderDto.UserId,
                CouponCode = checkoutHeaderDto.CouponCode,
                Discount = checkoutHeaderDto.DiscountTotal,
                OrderTotal = checkoutHeaderDto.OrderTotal,
                Name = $"{checkoutHeaderDto.FirstName} {checkoutHeaderDto.LastName}",
                Phone = checkoutHeaderDto.Phone,
                Email = checkoutHeaderDto.Email,
                OrderTime = DateTime.Now,
                Status = SD.Status_Pending,
                OrderDetails = new List<OrderDetails>()
            };

            foreach (var detail in checkoutHeaderDto.CartDetails)
            {
                OrderDetails orderDetail = new()
                {
                    ProductId = detail.ProductId,
                    ProductName = detail.Product?.Name ?? "",
                    Price = detail.Product?.Price ?? 0,
                    Count = detail.Count
                };
                ((List<OrderDetails>)orderHeader.OrderDetails).Add(orderDetail);
            }

            db.OrderHeaders.Add(orderHeader);
            await db.SaveChangesAsync();

            // Publish order created event for email etc.
            await _messageBus.PublishMessage(new OrderHeaderDto
            {
                OrderHeaderId = orderHeader.OrderHeaderId,
                UserId = orderHeader.UserId,
                CouponCode = orderHeader.CouponCode,
                Discount = orderHeader.Discount,
                OrderTotal = orderHeader.OrderTotal,
                Name = orderHeader.Name,
                Phone = orderHeader.Phone,
                Email = orderHeader.Email,
                OrderTime = orderHeader.OrderTime,
                Status = orderHeader.Status
            }, _orderCreatedTopic);
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
