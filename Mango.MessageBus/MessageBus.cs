using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Text;

namespace Mango.MessageBus
{
    public class MessageBus : IMessageBus
    {
        private readonly string connectionString = "Endpoint=sb://mangoweb-prince.servicebus.windows.net/;SharedAccessKeyName=RootManagedSharedAccessKey;SharedAccessKey=P3LDXgmp8RchqqwsqhIQ3GkfP5+9HWsNo+ASbPGjlFU=;EntityPath=emailshoppingcart";
        public async Task PublishMessage(object Message, string topic_queue_Name)
        {
            await using var client = new ServiceBusClient(connectionString);

            ServiceBusSender sender = client.CreateSender(topic_queue_Name);
            var jsonMessage = JsonConvert.SerializeObject(Message);
            ServiceBusMessage finalMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonMessage))
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            await sender.SendMessageAsync(finalMessage);
            await client.DisposeAsync();
        }
    }
}
