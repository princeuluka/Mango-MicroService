using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly IRabbitMQConnection _connection;

        public MessageBus(IRabbitMQConnection connection)
        {
            _connection = connection;
        }

        public async Task PublishMessage(object Message, string queueName)
        {
            using var channel = _connection.CreateChannel();
            
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var jsonMessage = JsonConvert.SerializeObject(Message);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body
            );

            await Task.CompletedTask;
        }
    }
}
