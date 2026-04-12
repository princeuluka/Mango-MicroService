using Mango.Services.ShoppingCartAPI.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mango.Services.ShoppingCartAPI.Extentions
{
    public static class ApplicationBuilderExtensions
    {
        private static IRabbitMQOrderConfirmedConsumer _rabbitMQOrderConfirmedConsumer;

        public static IApplicationBuilder UseRabbitMQConsumer(this IApplicationBuilder app)
        {
            _rabbitMQOrderConfirmedConsumer = app.ApplicationServices.GetService<IRabbitMQOrderConfirmedConsumer>();
            var hostApplicationLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            hostApplicationLife?.ApplicationStarted.Register(OnStart);
            hostApplicationLife?.ApplicationStopping.Register(OnStop);

            return app;
        }

        private static void OnStart()
        {
            _rabbitMQOrderConfirmedConsumer?.Start();
        }

        private static void OnStop()
        {
            _rabbitMQOrderConfirmedConsumer?.Stop();
        }
    }
}
