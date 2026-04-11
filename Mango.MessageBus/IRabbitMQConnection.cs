using RabbitMQ.Client;

namespace Mango.MessageBus
{
    public interface IRabbitMQConnection : IDisposable
    {
        IModel CreateChannel();
        bool IsConnected { get; }
    }
}
