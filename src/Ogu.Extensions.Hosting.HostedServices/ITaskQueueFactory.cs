using System.Threading.Channels;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    ///     Provides methods for managing and accessing task queues.
    ///     The <see cref="ITaskQueueFactory"/> allows for the creation, retrieval, and inspection of task queues by name.
    /// </summary>
    public interface ITaskQueueFactory
    {
        /// <summary>
        /// Gets the number of task queues currently managed by the factory.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Retrieves the task queue with the specified name.
        ///     If the queue does not exist, it returns <c>null</c>.
        /// </summary>
        /// <param name="queueName">The name of the queue to retrieve.</param>
        /// <returns>The <see cref="ITaskQueue"/> associated with the given name, or <c>null</c> if not found.</returns>
        ITaskQueue Get(string queueName);

        /// <summary>
        ///     Retrieves an existing task queue by name, or creates a new one with the specified options if it does not exist.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="opts">The options for the new queue if it is created.</param>
        /// <returns>The <see cref="ITaskQueue"/> associated with the given name.</returns>
        ITaskQueue GetOrCreate(string queueName, BoundedChannelOptions opts);

        /// <summary>
        ///     Checks whether a task queue with the specified name exists.
        /// </summary>
        /// <param name="queueName">The name of the queue to check.</param>
        /// <returns><c>true</c> if a queue with the specified name exists; otherwise, <c>false</c>.</returns>
        bool Contains(string queueName);

        /// <summary>
        ///     Gets the names of all task queues currently managed by the factory.
        /// </summary>
        /// <returns>An array of queue names.</returns>
        string[] GetQueueNames();
    }
    }