using System.Threading.Tasks;

namespace Mango.Services.ShoppingCartAPI.Messaging
{
    public interface IRabbitMQOrderConfirmedConsumer
    {
        Task Start();
        Task Stop();
    }
}
