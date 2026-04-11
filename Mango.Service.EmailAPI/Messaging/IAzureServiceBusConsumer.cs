namespace Mango.Service.EmailAPI.Messaging
{
    public interface IRabbitMQConsumer
    {
        Task Start();
        Task Stop();
    }
}
