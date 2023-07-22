using Mango.Service.EmailAPI.Messaging;

namespace Mango.Service.EmailAPI.Extension
{
    public static class ApplicationBuilderExtensions
    {

        private static IAzureServiceBusConsumer? serviceBusConsumer { get; set; }
        public static IApplicationBuilder UseAzureServiceBusConsumer(this IApplicationBuilder app)
        {
            serviceBusConsumer = app.ApplicationServices.GetService<IAzureServiceBusConsumer>();
            IHostApplicationLifetime? hostApplicationLife = app.ApplicationServices.GetService<IHostApplicationLifetime>();


            hostApplicationLife?.ApplicationStarted.Register(OnStart);
            hostApplicationLife?.ApplicationStarted.Register(OnStop);

            return app;
        }

        private static void OnStop()
        {
            serviceBusConsumer?.Stop();
        }

        private static void OnStart()
        {
            serviceBusConsumer?.Start();
        }
    }
}
