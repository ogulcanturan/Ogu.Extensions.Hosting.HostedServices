using Microsoft.Extensions.Hosting;
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
    /// Implements <see cref="IHostedService"/> to provide background task execution
    /// and <see cref="IDisposable"/> for proper cleanup of resources.
    /// </summary>
    public class TimedHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;
        private bool _disposed;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Func<CancellationToken, ValueTask> _task;
        private readonly ILogger _logger;
        private readonly string _worker;
        private readonly TimedHostedServiceOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedHostedService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log messages for the service.</param>
        /// <param name="worker">The name of the worker, used for identifying the service instance.</param>
        /// <param name="task">A function that takes a <see cref="CancellationToken"/> and returns a <see cref="ValueTask"/> to be executed by the worker.</param>
        /// <param name="options">An optional action to configure the options for the timed hosted service.</param>
        public TimedHostedService(ILogger logger, string worker, Func<CancellationToken, ValueTask> task, Action<TimedHostedServiceOptions> options = null)
        {
            _logger = logger ?? new NullLogger<TimedHostedService>();
            _worker = worker;
            _task = task;
            _options = new TimedHostedServiceOptions();
            options?.Invoke(_options);
        }

        public virtual bool IsExecuting { get; private set; }
        public virtual bool HasStarted { get; private set; }
        public virtual DateTime? NextTaskAt { get; private set; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            InternalLogs.WorkerStartPlanned(_logger, _worker, DateTime.UtcNow.Add(_options.StartsIn), _options.Period, null);

            HasStarted = true;

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _timer = new Timer(x => _executingTask = DoWorkAsync(x, _stoppingCts.Token), null, _options.StartsIn, _options.Period);

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            InternalLogs.WorkerStopping(_logger, _worker, null);

            if (_executingTask == null)
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
                await _executingTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
                // Wait until the task completes or the stop token triggers
                var tcs = new TaskCompletionSource<object>();
#if !NETSTANDARD2_0
                await
#endif
                using (var registration = cancellationToken.Register(s => ((TaskCompletionSource<object>)s).SetCanceled(), tcs))
                {
                    // Do not await the _executeTask because cancelling it will throw an OperationCanceledException which we are explicitly ignoring
                    await Task.WhenAny(_executingTask, tcs.Task).ConfigureAwait(false);
                }
#endif
                HasStarted = false;
                NextTaskAt = null;

                InternalLogs.WorkerStopped(_logger, _worker, null);
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
            }

            _disposed = true;
        }

        private async Task DoWorkAsync(object state, CancellationToken cancellationToken)
        {
            NextTaskAt = DateTime.UtcNow.Add(_options.Period);

            if (IsExecuting || !await _semaphore.WaitAsync(0, cancellationToken))
            {
                InternalLogs.SkippingTask(_logger, _worker, null);

                return;
            }

            var taskUniqueId = InternalHelpers.GetTaskUniqueId();

            InternalLogs.TaskStarted(_logger, _worker, taskUniqueId, null);

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
                            await _task(linkedCts.Token).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await _task(cancellationToken).ConfigureAwait(false);
                }

                stop = Stopwatch.GetTimestamp();
                status = InternalConstants.Success;
            }
            catch (OperationCanceledException)
            {
                stop = Stopwatch.GetTimestamp();

                if (timeoutCts?.Token.IsCancellationRequested == true)
                {
                    InternalLogs.ExecuteException(_logger, _worker, taskUniqueId, InternalConstants.TaskTimedOut);
                    status = InternalConstants.Failure;
                }
                else
                {
                    InternalLogs.ExecuteException(_logger, _worker, taskUniqueId, InternalConstants.TaskCanceled);
                    status = InternalConstants.Failure;
                }
            }
            catch (Exception ex)
            {
                stop = Stopwatch.GetTimestamp();
                InternalLogs.ExecuteException(_logger, _worker, taskUniqueId, ex);
                status = InternalConstants.Failure;
            }
         
            InternalLogs.TaskCompletedWithNext(_logger, _worker, taskUniqueId, status, InternalHelpers.GetElapsedMilliseconds(start, stop), NextTaskAt.Value, null);

            IsExecuting = false;

            _semaphore.Release();

            if (_options.PreservePeriod)
            {
                NextTaskAt = DateTime.UtcNow.Add(_options.Period);
                _timer?.Change(_options.Period, Timeout.InfiniteTimeSpan);
            }
        }
    }
}