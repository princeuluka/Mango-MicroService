using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mango.MessageBus
{
    public class RabbitMQConnection : IRabbitMQConnection
    {
        private readonly IConnection _connection;
        private readonly ILogger<RabbitMQConnection>? _logger;
        private bool _disposed;

        public RabbitMQConnection(IConfiguration configuration, ILogger<RabbitMQConnection>? logger = null)
        {
            _logger = logger;
            
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"],
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            var port = configuration.GetValue<int?>("RabbitMQ:Port");
            if (port.HasValue)
            {
                factory.Port = port.Value;
            }

            _logger?.LogInformation("Connecting to RabbitMQ at {HostName}:{Port}", factory.HostName, factory.Port);
            
            _connection = factory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.CallbackException += OnCallbackException;
            _connection.ConnectionBlocked += OnConnectionBlocked;
            _connection.ConnectionUnblocked += OnConnectionUnblocked;
            
            _logger?.LogInformation("RabbitMQ connection established");
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IModel CreateChannel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connection available");
            }
            return _connection.CreateModel();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger?.LogWarning("RabbitMQ connection shutdown. ReplyCode: {ReplyCode}, ReplyText: {ReplyText}", 
                e.ReplyCode, e.ReplyText);
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "RabbitMQ callback exception: {Detail}", e.Detail);
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            _logger?.LogWarning("RabbitMQ connection blocked. Reason: {Reason}", e.Reason);
        }

        private void OnConnectionUnblocked(object sender, EventArgs e)
        {
            _logger?.LogInformation("RabbitMQ connection unblocked");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing RabbitMQ connection");
            }
        }
    }
}
