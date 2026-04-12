using System.Threading.Tasks;

namespace Mango.Services.OrderAPI.Messaging
{
    public interface IRabbitMQConsumer
    {
        Task Start();
        Task Stop();
    }
}
