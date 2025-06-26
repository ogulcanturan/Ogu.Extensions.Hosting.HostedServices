namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    /// Represents logging options for a timed hosted service, allowing fine-grained control
    /// over what lifecycle events should be logged.
    /// </summary>
    public class TimedHostedServiceLogOptions
    {
        /// <summary>
        /// Logs when the worker is scheduled to start its background task (before execution).
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenWorkerStartPlanned { get; set; } = true;

        /// <summary>
        /// Logs when the background task actually starts execution.
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenTaskStarted { get; set; } = true;

        /// <summary>
        /// Logs when the background task completes execution successfully.
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenTaskCompleted { get; set; } = true;

        /// <summary>
        /// Logs when the worker is in the process of stopping (usually triggered by cancellation).
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenWorkerStopping { get; set; } = true;

        /// <summary>
        /// Logs when the worker has fully stopped.
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenWorkerStopped { get; set; } = true;

        /// <summary>
        /// Logs when an unhandled exception is caught during task execution.
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenCaughtAnException { get; set; } = true;

        /// <summary>
        /// Logs when a scheduled task execution is skipped, typically because the previous task is still running.
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenSkippingTask { get; set; } = true;

        /// <summary>
        /// Logs when the worker is already started.
        /// </summary>
        /// <remarks>Default is <c>true</c></remarks>
        public bool LogWhenWorkerHasAlreadyStarted { get; set; } = true;

        /// <summary>
        /// Logs when the worker is starting after being disposed.
        /// </summary>
        public bool LogWhenStartingDisposedWorker { get; set; } = true;

        /// <summary>
        /// Logs when the worker is stopping after being disposed.
        /// </summary>
        public bool LogWhenStoppingDisposedWorker { get; set; } = true;
    }
}