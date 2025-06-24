using System;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    /// Represents the configuration options for the QueueHostedService.
    /// </summary>
    public class TaskQueueHostedServiceOptions
    {
        /// <summary>
        /// Gets or sets the maximum time allowed for the task to complete.
        /// If the task does not complete within this time, it will trigger the cancellation.
        /// If set to <c>null</c> (default), the timeout is not enabled, and the task can run indefinitely.
        /// </summary>
        public TimeSpan? TaskTimeout { get; set; }

        /// <summary>
        /// Gets or sets the logging options for the task queue hosted service.
        /// </summary>
        public TaskQueueHostedServiceLogOptions LogOptions { get; set; } = new TaskQueueHostedServiceLogOptions();
    }
}