using Mango.Services.RewardAPI.Data;
using Mango.Services.RewardAPI.Models;
using Mango.Services.RewardAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.RewardAPI.Messaging
{
    public class RabbitMQConsumer : IRabbitMQConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly DbContextOptions<AppDbContext> _dbOptions;
        private readonly string _orderCreatedTopic;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMQConsumer(IConfiguration configuration, DbContextOptions<AppDbContext> dbOptions)
        {
            _configuration = configuration;
            _dbOptions = dbOptions;
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

            _channel.QueueDeclare(queue: _orderCreatedTopic, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(json);

                    await ProcessOrderCreated(orderHeaderDto);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing order created for rewards: {ex.Message}");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _orderCreatedTopic, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessOrderCreated(OrderHeaderDto? orderHeaderDto)
        {
            if (orderHeaderDto == null || orderHeaderDto.OrderTotal <= 0) return;

            using var db = new AppDbContext(_dbOptions);

            int points = (int)orderHeaderDto.OrderTotal;

            Rewards reward = new()
            {
                OrderId = orderHeaderDto.OrderHeaderId,
                UserId = orderHeaderDto.UserId,
                RewardsDate = DateTime.Now,
                RewardsPoints = points
            };

            db.Rewards.Add(reward);
            await db.SaveChangesAsync();
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
