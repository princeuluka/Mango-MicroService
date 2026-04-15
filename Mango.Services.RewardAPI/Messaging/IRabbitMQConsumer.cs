using System.Threading.Tasks;

namespace Mango.Services.RewardAPI.Messaging
{
    public interface IRabbitMQConsumer
    {
        Task Start();
        Task Stop();
    }
}
