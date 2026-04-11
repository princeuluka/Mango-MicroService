namespace Mango.Service.EmailAPI.Messaging
{
    public interface IMessageConsumer
    {
        Task Start();
        Task Stop();
    }
}
