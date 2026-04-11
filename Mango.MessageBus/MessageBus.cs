using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly IConfiguration _configuration;
        private static IConnectionFactory? _connectionFactory;
        private static IConnection? _connection;
        private static readonly object _lock = new();

        public MessageBus(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IConnection GetConnection()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                lock (_lock)
                {
                    if (_connection == null || !_connection.IsOpen)
                    {
                        if (_connectionFactory == null)
                        {
                            _connectionFactory = new ConnectionFactory
                            {
                                HostName = _configuration["MessageBus:Host"] ?? "localhost",
                                Port = int.TryParse(_configuration["MessageBus:Port"], out var p) ? p : 5672,
                                UserName = _configuration["MessageBus:UserName"] ?? "guest",
                                Password = _configuration["MessageBus:Password"] ?? "guest"
                            };
                        }
                        _connection = _connectionFactory.CreateConnection();
                    }
                }
            }
            return _connection;
        }

        public Task PublishMessage(object Message, string topic_queue_Name)
        {
            using var channel = GetConnection().CreateModel();
            channel.QueueDeclare(queue: topic_queue_Name, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var jsonMessage = JsonConvert.SerializeObject(Message);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(exchange: "", routingKey: topic_queue_Name, basicProperties: properties, body: body);
            return Task.CompletedTask;
        }
    }
}
