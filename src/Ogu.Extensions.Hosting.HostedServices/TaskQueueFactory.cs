using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;

namespace Ogu.Extensions.Hosting.HostedServices
{
    public class TaskQueueFactory : ITaskQueueFactory
    {
        private readonly ConcurrentDictionary<string, ITaskQueue> _queueNameToTaskQueue = new ConcurrentDictionary<string, ITaskQueue>();

        public int Count => _queueNameToTaskQueue.Count;

        public ITaskQueue Get(string queueName)
        {
            return _queueNameToTaskQueue.TryGetValue(queueName, out var taskQueue) ? taskQueue : null;
        }

        public ITaskQueue GetOrCreate(string queueName, BoundedChannelOptions opts)
        {
            if (_queueNameToTaskQueue.TryGetValue(queueName, out var taskQueue))
            {
                return taskQueue;
            }

            taskQueue = new TaskQueue(opts);

            _queueNameToTaskQueue[queueName] = taskQueue;

            return taskQueue;
        }

        public bool Contains(string queueName)
        {
            return _queueNameToTaskQueue.ContainsKey(queueName);
        }

        public string[] GetQueueNames()
        {
            return _queueNameToTaskQueue.Keys.ToArray();
        }
    }
}