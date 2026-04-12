namespace Mango.Services.OrderAPI.Messaging
{
    public interface IRabbitMQCheckoutConsumer
    {
        Task Start();
        Task Stop();
    }
}
