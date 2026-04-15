using Mango.Services.RewardAPI.Messaging;

namespace Mango.Services.RewardAPI.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        private static IRabbitMQConsumer rabbitMQConsumer;

        public static IApplicationBuilder UseRabbitMQConsumer(this IApplicationBuilder app)
        {
            rabbitMQConsumer = app.ApplicationServices.GetService<IRabbitMQConsumer>();
            var hostApplicationLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            hostApplicationLife?.ApplicationStarted.Register(OnStart);
            hostApplicationLife?.ApplicationStopping.Register(OnStop);

            return app;
        }

        private static void OnStop()
        {
            rabbitMQConsumer?.Stop();
        }

        private static void OnStart()
        {
            rabbitMQConsumer?.Start();
        }
    }
}
