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
        /// </summary>
        public TimeSpan? TaskTimeout { get; set; }
    }
}