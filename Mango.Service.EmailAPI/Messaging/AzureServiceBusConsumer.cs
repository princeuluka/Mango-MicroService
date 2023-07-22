using Azure.Messaging.ServiceBus;
using Mango.Service.EmailAPI.Services;
using Mango.Services.EmailAPI.Models.Dto;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Service.EmailAPI.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string emailCartQueue;
        private readonly IConfiguration? _configuration;
        private readonly ServiceBusProcessor _emailCartProcessor;
        private readonly EmailService _emailService;

        public AzureServiceBusConsumer(IConfiguration? configuration, EmailService emailService)
        {
            _configuration = configuration;
            serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");

            emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");

            var client = new ServiceBusClient(serviceBusConnectionString);
            _emailCartProcessor = client.CreateProcessor(emailCartQueue);
            _emailService = emailService;
        }

        public async Task Start()
        {
            _emailCartProcessor.ProcessMessageAsync += OnEmailCartRequestRecieved;
            _emailCartProcessor.ProcessMessageAsync += ErrorHandler;
            await _emailCartProcessor.StartProcessingAsync();

        }
        public async Task Stop()
        {
            await _emailCartProcessor.StopProcessingAsync();
            await _emailCartProcessor.DisposeAsync();
        }

        private Task ErrorHandler(ProcessMessageEventArgs args)
        {
            Console.WriteLine(args.Message.ToString());
            return Task.CompletedTask;
        }

        private async Task OnEmailCartRequestRecieved(ProcessMessageEventArgs args)
        {
            // this is where you will recieve the message
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            CartDto objMessage = JsonConvert.DeserializeObject<CartDto>(body);
            try
            {
                // TODO - try to log email
               await  _emailService.EmailCartAndLog(objMessage);
                await args.CompleteMessageAsync(args.Message);

            }
            catch (Exception ex)
            {

                throw;
            }

        }

        
    }
}
