using System.Threading.Channels;

namespace Ogu.Extensions.Hosting.HostedServices
{
    public interface ITaskQueueFactory
    {
        int Count { get; }

        ITaskQueue Get(string queueName);

        ITaskQueue GetOrCreate(string queueName, BoundedChannelOptions opts);

        bool Contains(string queueName);

        string[] GetQueueNames();
    }
}