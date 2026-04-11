using Mango.Service.EmailAPI.Messaging;

namespace Mango.Service.EmailAPI.Extension
{
    public static class ApplicationBuilderExtensions
    {

        private static IRabbitMQConsumer? rabbitMQConsumer { get; set; }
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
