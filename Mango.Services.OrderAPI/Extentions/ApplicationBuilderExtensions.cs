using Mango.Services.OrderAPI.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mango.Services.OrderAPI.Extentions
{
    public static class ApplicationBuilderExtensions
    {
        private static IRabbitMQCheckoutConsumer _rabbitMQCheckoutConsumer;

        public static IApplicationBuilder UseRabbitMQConsumer(this IApplicationBuilder app)
        {
            _rabbitMQCheckoutConsumer = app.ApplicationServices.GetService<IRabbitMQCheckoutConsumer>();
            var hostApplicationLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            hostApplicationLife?.ApplicationStarted.Register(OnStart);
            hostApplicationLife?.ApplicationStopping.Register(OnStop);

            return app;
        }

        private static void OnStart()
        {
            _rabbitMQCheckoutConsumer?.Start();
        }

        private static void OnStop()
        {
            _rabbitMQCheckoutConsumer?.Stop();
        }
    }
}
