using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    ///     A concrete implementation of the <see cref="ITaskQueue"/> interface.
    ///     Manages the queuing and dequeuing of tasks to be executed asynchronously.
    /// </summary>
    public sealed class TaskQueue : ITaskQueue
    {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

        public TaskQueue(int capacity) : this(new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait })
        {
        }

        public TaskQueue(BoundedChannelOptions options)
        {
            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        }

        public ValueTask QueueTaskAsync(Func<CancellationToken, ValueTask> task, CancellationToken cancellationToken = default)
        {
            return _queue.Writer.WriteAsync(task, cancellationToken);
        }

        public ValueTask<Func<CancellationToken, ValueTask>> DequeueTaskAsync(CancellationToken cancellationToken = default)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}