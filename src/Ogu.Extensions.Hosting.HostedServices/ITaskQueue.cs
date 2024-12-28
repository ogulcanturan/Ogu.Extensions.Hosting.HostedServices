using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    ///     Defines the operations for managing a task queue.
    ///     Provides methods for queuing tasks and dequeuing them asynchronously.
    /// </summary>
    public interface ITaskQueue
    {
        /// <summary>
        ///     Enqueues a task to be executed asynchronously.
        /// </summary>
        /// <param name="task">The task to be queued, represented as a function that takes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to signal the cancellation of the task operation (optional).</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation of queuing the task.</returns>
        ValueTask QueueTaskAsync(Func<CancellationToken, ValueTask> task, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Dequeues a task to be executed asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to signal the cancellation of the dequeue operation (optional).</param>
        /// <returns>A <see cref="ValueTask"/> that returns a function representing the dequeued task.</returns>
        ValueTask<Func<CancellationToken, ValueTask>> DequeueTaskAsync(CancellationToken cancellationToken = default);
    }
}