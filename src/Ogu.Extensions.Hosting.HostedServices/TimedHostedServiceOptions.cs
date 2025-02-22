using System;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    /// Represents the configuration options for the TimedHostedService.
    /// </summary>
    public class TimedHostedServiceOptions
    {
        internal event Action<string> OnPropertyChanged;

        private TimeSpan _period = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the period (interval) between each task execution.
        /// The default value is 10 seconds.
        /// </summary>
        public TimeSpan Period
        {
            get => _period;
            set
            {
                if (_period == value)
                {
                    return;
                }

                _period = value;
                OnPropertyChanged?.Invoke(nameof(Period));
            }
        }

        private TimeSpan _startsIn = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the delay before the first task execution starts.
        /// The default value is <see cref="TimeSpan.Zero"/>, meaning the task starts immediately.
        /// </summary>
        public TimeSpan StartsIn
        {
            get => _startsIn;
            set
            {
                if (_startsIn == value)
                {
                    return;
                }

                _startsIn = value;
                OnPropertyChanged?.Invoke(nameof(StartsIn));
            }
        }

        private TimeSpan? _taskTimeout;

        /// <summary>
        /// Gets or sets the maximum time allowed for the task to complete.
        /// If the task does not complete within this time, it will trigger the cancellation.
        /// If set to <c>null</c> (default), the timeout is not enabled, and the task can run indefinitely.
        /// </summary>
        public TimeSpan? TaskTimeout
        {
            get => _taskTimeout;
            set
            {
                if(_taskTimeout == value)
                {
                    return;
                }

                _taskTimeout = value;
                OnPropertyChanged?.Invoke(nameof(TaskTimeout));
            }
        }

        private bool _preservePeriod;

        /// <summary>
        /// Gets or sets a value that specifies whether the task execution interval should be preserved.
        /// </summary>
        /// <remarks>
        /// When this property is set to <c>true</c>, the next task execution will occur after the specified period 
        /// has passed since the previous task completion. When set to <c>false</c>, the next task execution will depend 
        /// on the next timer tick, and is not guaranteed to start immediately after the previous task finishes.
        /// </remarks>
        public bool PreservePeriod { 
            get => _preservePeriod;
            set
            {
                if (_preservePeriod == value)
                {
                    return;
                }

                _preservePeriod = value;
                OnPropertyChanged?.Invoke(nameof(PreservePeriod));
            }
        }
    }
}