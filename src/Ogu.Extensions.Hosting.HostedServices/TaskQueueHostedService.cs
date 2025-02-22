using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.Extensions.Hosting.HostedServices
{
    /// <summary>
    /// A background service that processes tasks from a queue asynchronously.
    /// Implements <see cref="ITaskQueueHostedService"/> to run as a hosted service and manage task execution from the queue.
    /// </summary>
    public class TaskQueueHostedService : ITaskQueueHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;
        private bool _disposed;

        private readonly object _lock = new object();
        private readonly ILogger _logger;
        private readonly string _worker;
        private readonly ITaskQueue _taskQueue;
        private readonly TaskQueueHostedServiceOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskQueueHostedService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log messages for the service.</param>
        /// <param name="worker">The name of the worker, used for identifying the service instance.</param>
        /// <param name="taskQueue">The <see cref="ITaskQueue"/> instance that represents the task queue to be processed by the service.</param>
        /// <param name="options">An optional action to configure the options for the task queue hosted service.</param>
        public TaskQueueHostedService(ILogger logger, string worker, ITaskQueue taskQueue, Action<TaskQueueHostedServiceOptions> options = null)
        {
            _logger = logger ?? new NullLogger<TaskQueueHostedService>();
            _worker = worker;
            _taskQueue = taskQueue;
            _options = new TaskQueueHostedServiceOptions();
            options?.Invoke(_options);
        }

        public virtual bool IsExecuting { get; private set; }
        public virtual bool HasStarted { get; private set; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            InternalLogs.WorkerStarted(_logger, _worker, null);

            HasStarted = true;

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = DoWorkAsync(cancellationToken);

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

                InternalLogs.WorkerStopped(_logger, _worker, null);
            }
        }

        public void UpdateOptions(Action<TaskQueueHostedServiceOptions> updateAction)
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
            }

            _disposed = true;
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            while (!_stoppingCts.IsCancellationRequested)
            {
                var taskUniqueId = InternalHelpers.GetTaskUniqueId();

                IsExecuting = true;

                long stop, start = 0;
                string status;

                try
                {
                    var task = await _taskQueue.DequeueTaskAsync(cancellationToken).ConfigureAwait(false);

                    InternalLogs.TaskStarted(_logger, _worker, taskUniqueId, null);

                    start = Stopwatch.GetTimestamp();

                    if (_options.TaskTimeout.HasValue)
                    {
                        using (var timeoutCts = new CancellationTokenSource(_options.TaskTimeout.Value))
                        {
                            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                            {
                                _executingTask = task(linkedCts.Token).AsTask();

                                await _executingTask.ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        _executingTask = task(cancellationToken).AsTask();

                        await _executingTask.ConfigureAwait(false);
                    }

                    stop = Stopwatch.GetTimestamp();
                    status = InternalConstants.Success;
                }
                catch (OperationCanceledException)
                {
                    stop = Stopwatch.GetTimestamp();

                    InternalLogs.ExecuteException(_logger, _worker, taskUniqueId, InternalConstants.TaskCanceled);
                    status = InternalConstants.Failure;
                }
                catch (Exception ex)
                {
                    stop = Stopwatch.GetTimestamp();
                    InternalLogs.ExecuteException(_logger, _worker, taskUniqueId, ex);
                    status = InternalConstants.Failure;
                }

                InternalLogs.TaskCompleted(_logger, _worker, taskUniqueId, status, start == 0 ? 0 : InternalHelpers.GetElapsedMilliseconds(start, stop), null);

                IsExecuting = false;
            }
        }
    }
}