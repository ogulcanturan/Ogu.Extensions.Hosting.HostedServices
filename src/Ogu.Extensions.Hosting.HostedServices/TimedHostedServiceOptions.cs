using System;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    ///     Represents the configuration options for the TimedHostedService.
    /// </summary>
    public class TimedHostedServiceOptions
    {
        /// <summary>
        ///     Gets or sets the period (interval) between each task execution.
        ///     The default value is 10 seconds.
        /// </summary>
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        ///     Gets or sets the delay before the first task execution starts.
        ///     The default value is <see cref="TimeSpan.MinValue"/>, meaning the task starts immediately.
        /// </summary>
        public TimeSpan StartsIn { get; set; } = TimeSpan.MinValue;

        /// <summary>
        ///     Gets or sets the maximum time allowed for the task to complete.
        ///     If the task does not complete within this time, it will trigger the cancellation.
        /// </summary>
        public TimeSpan? TaskTimeout { get; set; }

        /// <summary>
        ///     Gets or sets a value that specifies whether the task execution interval should be preserved.
        /// </summary>
        /// <remarks>
        ///     When this property is set to <c>true</c>, the next task execution will occur after the specified period 
        ///     has passed since the previous task completion. When set to <c>false</c>, the next task execution will depend 
        ///     on the next timer tick, and is not guaranteed to start immediately after the previous task finishes.
        /// </remarks>
        public bool PreservePeriod { get; set; }
    }
}