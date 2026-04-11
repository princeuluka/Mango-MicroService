using Mango.Service.EmailAPI.Messaging;

namespace Mango.Service.EmailAPI.Extension
{
    public static class ApplicationBuilderExtensions
    {

        private static IMessageConsumer? messageConsumer { get; set; }
        public static IApplicationBuilder UseRabbitMQConsumer(this IApplicationBuilder app)
        {
            messageConsumer = app.ApplicationServices.GetService<IMessageConsumer>();
            var hostApplicationLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();


            hostApplicationLife?.ApplicationStarted.Register(OnStart);
            hostApplicationLife?.ApplicationStopping.Register(OnStop);

            return app;
        }

        private static void OnStop()
        {
            messageConsumer?.Stop();
        }

        private static void OnStart()
        {
            messageConsumer?.Start();
        }
    }
}
