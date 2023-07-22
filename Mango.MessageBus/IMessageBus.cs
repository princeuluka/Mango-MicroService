namespace Mango.MessageBus
{
    public interface IMessageBus
    {
        Task PublishMessage(object Message, string topic_queue_Name);
    }
}
