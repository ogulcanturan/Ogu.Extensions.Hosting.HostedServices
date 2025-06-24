using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    /// Represents a background service that executes tasks at regular, timed intervals.
    /// Implements <see cref="ITimedHostedService"/> to provide background task execution
    /// and <see cref="IDisposable"/> for proper cleanup of resources.
    /// </summary>
    public class TimedHostedService : ITimedHostedService, IDisposable
    {
        private Timer _timer;
        private ValueTask _executingTask;
        private CancellationTokenSource _stoppingCts;
        private bool _disposed;
        private bool _periodUpdated;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly object _lock = new object();
        private readonly Func<ITimedHostedService, CancellationToken, ValueTask> _task;
        private readonly string _worker;
        private readonly TimedHostedServiceOptions _options;

        protected readonly ILogger Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedHostedService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log messages for the service.</param>
        /// <param name="worker">The name of the worker, used for identifying the service instance.</param>
        /// <param name="options">An optional action to configure the options for the timed hosted service.</param>
        protected TimedHostedService(ILogger logger, string worker, Action<TimedHostedServiceOptions> options = null)
        {
            Logger = logger ?? new NullLogger<TimedHostedService>();
            _worker = worker;
            _options = new TimedHostedServiceOptions();
            options?.Invoke(_options);
            _options.OnPropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedHostedService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log messages for the service.</param>
        /// <param name="worker">The name of the worker, used for identifying the service instance.</param>
        /// <param name="task">A function that takes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/> to be executed by the worker.</param>
        /// <param name="options">An optional action to configure the options for the timed hosted service.</param>
        public TimedHostedService(ILogger logger, string worker, Func<CancellationToken, ValueTask> task, Action<TimedHostedServiceOptions> options = null)
        {
            Logger = logger ?? new NullLogger<TimedHostedService>();
            _worker = worker;
            _task = (timedHostedService, cancellationToken) => task(cancellationToken);
            _options = new TimedHostedServiceOptions();
            options?.Invoke(_options);
            _options.OnPropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedHostedService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log messages for the service.</param>
        /// <param name="worker">The name of the worker, used for identifying the service instance.</param>
        /// <param name="task">A function that takes <see cref="ITimedHostedService"/> and <see cref="CancellationToken"/> then returns a <see cref="ValueTask"/> to be executed by the worker.</param>
        /// <param name="options">An optional action to configure the options for the timed hosted service.</param>
        public TimedHostedService(ILogger logger, string worker, Func<ITimedHostedService, CancellationToken, ValueTask> task, Action<TimedHostedServiceOptions> options = null)
        {
            Logger = logger ?? new NullLogger<TimedHostedService>();
            _worker = worker;
            _task = task;
            _options = new TimedHostedServiceOptions();
            options?.Invoke(_options);
            _options.OnPropertyChanged += OnPropertyChanged;
        }

        public virtual bool IsExecuting { get; private set; }
        public virtual bool HasStarted { get; private set; }
        public virtual DateTime? NextTaskAt { get; private set; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            if (_options.LogOptions.LogWhenWorkerStartPlanned)
            {
                InternalLogs.WorkerStartPlanned(Logger, _worker, DateTime.UtcNow.Add(_options.StartsIn), _options.Period, null);
            }

            HasStarted = true;

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _timer = new Timer(x => _executingTask = PrivateWorkAsync(x, _stoppingCts.Token), null, _options.StartsIn, _options.Period);

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_options.LogOptions.LogWhenWorkerStopping)
            {
                InternalLogs.WorkerStopping(Logger, _worker, null);
            }

            var executingTask = _executingTask.AsTask();

            if (executingTask == null)
            {
                return;
            }

            try
            {
                _stoppingCts?.Cancel();
                _timer?.Change(Timeout.Infinite, 0);
            }
            finally
            {
#if NET8_0_OR_GREATER
                await executingTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
                // Wait until the task completes or the stop token triggers
                var tcs = new TaskCompletionSource<object>();
#if !NETSTANDARD2_0
                await
#endif
                using (var registration = cancellationToken.Register(s => ((TaskCompletionSource<object>)s).SetCanceled(), tcs))
                {
                    // Do not await the _executeTask because cancelling it will throw an OperationCanceledException which we are explicitly ignoring
                    await Task.WhenAny(executingTask, tcs.Task).ConfigureAwait(false);
                }
#endif
                HasStarted = false;
                NextTaskAt = null;

                if (_options.LogOptions.LogWhenWorkerStopped)
                {
                    InternalLogs.WorkerStopped(Logger, _worker, null);
                }
            }
        }

        public void UpdateOptions(Action<TimedHostedServiceOptions> updateAction)
        {
            lock (_lock)
            {
                updateAction(_options);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _stoppingCts?.Cancel();
                _timer?.Dispose();
                _options.OnPropertyChanged -= OnPropertyChanged;
            }

            _disposed = true;
        }

        /// <summary>
        /// Executes the work of the timed hosted service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="ValueTask"/> representing the operation. It may complete synchronously.
        /// </returns>
        protected virtual async ValueTask DoWorkAsync(CancellationToken cancellationToken)
        {
            if (_task == null)
            {
                return;
            }

            await _task(this, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask PrivateWorkAsync(object state, CancellationToken cancellationToken)
        {
            NextTaskAt = DateTime.UtcNow.Add(_options.Period);

            if (IsExecuting || !await _semaphore.WaitAsync(0, cancellationToken))
            {
                if (_options.LogOptions.LogWhenSkippingTask)
                {
                    InternalLogs.SkippingTask(Logger, _worker, null);
                }

                return;
            }

            var taskUniqueId = InternalHelpers.GetTaskUniqueId();

            if (_options.LogOptions.LogWhenTaskStarted)
            {
                InternalLogs.TaskStarted(Logger, _worker, taskUniqueId, null);
            }

            IsExecuting = true;

            long stop;
            var start = Stopwatch.GetTimestamp();
            string status;
            CancellationTokenSource timeoutCts = null;

            try
            {
                if (_options.TaskTimeout.HasValue)
                {
                    using (timeoutCts = new CancellationTokenSource(_options.TaskTimeout.Value))
                    {
                        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                        {
                            await DoWorkAsync(linkedCts.Token).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await DoWorkAsync(cancellationToken).ConfigureAwait(false);
                }

                stop = Stopwatch.GetTimestamp();
                status = InternalConstants.Success;
            }
            catch (OperationCanceledException)
            {
                stop = Stopwatch.GetTimestamp();

                if (timeoutCts?.Token.IsCancellationRequested == true)
                {
                    if (_options.LogOptions.LogWhenCaughtAnException)
                    {
                        InternalLogs.CaughtAnException(Logger, _worker, taskUniqueId, InternalConstants.TaskTimedOut);
                    }
                }
                else if (_options.LogOptions.LogWhenCaughtAnException)
                {
                    InternalLogs.CaughtAnException(Logger, _worker, taskUniqueId, InternalConstants.TaskCanceled);
                }

                status = InternalConstants.Failure;
            }
            catch (Exception ex)
            {
                stop = Stopwatch.GetTimestamp();

                if (_options.LogOptions.LogWhenCaughtAnException)
                {
                    InternalLogs.CaughtAnException(Logger, _worker, taskUniqueId, ex);
                }

                status = InternalConstants.Failure;
            }

            var elapsedMilliseconds = InternalHelpers.GetElapsedMilliseconds(start, stop);

            if (_options.LogOptions.LogWhenTaskCompleted)
            {
                InternalLogs.TaskCompletedWithNext(Logger, _worker, taskUniqueId, status, elapsedMilliseconds, NextTaskAt.Value, null);
            }

            IsExecuting = false;

            _semaphore.Release();

            if (_options.PreservePeriod)
            {
                NextTaskAt = DateTime.UtcNow.Add(_options.Period);
                _timer?.Change(_options.Period, Timeout.InfiniteTimeSpan);
            }
            else if (_periodUpdated)
            {
                var dueTime = _options.Period.Add(TimeSpan.FromMilliseconds(elapsedMilliseconds));

                NextTaskAt = DateTime.UtcNow.Add(dueTime);
                _timer?.Change(dueTime, _options.Period);

                _periodUpdated = false;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(TimedHostedServiceOptions.StartsIn) when !HasStarted && NextTaskAt == null:

                    _timer?.Change(_options.StartsIn, _options.Period);

                    break;

                case nameof(TimedHostedServiceOptions.Period) when !_options.PreservePeriod:

                    _periodUpdated = true;

                    break;
            }
        }
    }
}