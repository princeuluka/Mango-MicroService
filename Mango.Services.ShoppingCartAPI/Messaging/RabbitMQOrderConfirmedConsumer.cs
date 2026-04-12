using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mango.Services.ShoppingCartAPI.Messaging
{
    public class RabbitMQOrderConfirmedConsumer : IRabbitMQOrderConfirmedConsumer
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly string _orderConfirmedQueue;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQOrderConfirmedConsumer(IConfiguration configuration, AppDbContext db)
        {
            _configuration = configuration;
            _db = db;
            _orderConfirmedQueue = _configuration.GetValue<string>("TopicAndQueueNames:OrderConfirmedQueue") ?? "orderconfirmedqueue";
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

            _channel.QueueDeclare(queue: _orderConfirmedQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var orderHeaderDto = JsonConvert.DeserializeObject<OrderHeaderDto>(json);
                    await ClearCart(orderHeaderDto.UserId);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                    throw;
                }
            };

            _channel.BasicConsume(queue: _orderConfirmedQueue, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async Task ClearCart(string userId)
        {
            var cartHeaderFromDb = await _db.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
            if (cartHeaderFromDb != null)
            {
                var cartDetails = _db.CartDetails.Where(u => u.CartHeaderId == cartHeaderFromDb.CartHeaderId);
                _db.CartDetails.RemoveRange(cartDetails);
                _db.CartHeaders.Remove(cartHeaderFromDb);
                await _db.SaveChangesAsync();
            }
        }

        public Task Stop()
        {
            _channel?.Close();
            _connection?.Close();
            return Task.CompletedTask;
        }
    }
}
