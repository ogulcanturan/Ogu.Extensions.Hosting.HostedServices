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
    ///     A background service that processes tasks from a queue asynchronously.
    ///     Implements <see cref="IHostedService"/> to run as a hosted service and manage task execution from the queue.
    /// </summary>
    public class TaskQueueHostedService : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;
        private bool _disposed;

        private readonly ILogger _logger;
        private readonly ITaskQueue _taskQueue;
        private readonly TaskQueueHostedServiceOptions _options;

        public TaskQueueHostedService(ILogger logger, ITaskQueue taskQueue, Action<TaskQueueHostedServiceOptions> options = null)
        {
            _logger = logger ?? new NullLogger<TaskQueueHostedService>();
            _taskQueue = taskQueue;
            _options = new TaskQueueHostedServiceOptions();
            options?.Invoke(_options);
        }

        public virtual bool IsExecuting { get; private set; }
        public virtual bool HasStarted { get; private set; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            InternalLogs.WorkerStarted(_logger, null);

            HasStarted = true;

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = DoWorkAsync(cancellationToken);

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            InternalLogs.WorkerStopping(_logger, null);

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

                InternalLogs.WorkerStopped(_logger, null);
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

                    InternalLogs.TaskStarted(_logger, taskUniqueId, null);

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

                    InternalLogs.ExecuteException(_logger, taskUniqueId, InternalConstants.TaskCanceled);
                    status = InternalConstants.Failure;
                }
                catch (Exception ex)
                {
                    stop = Stopwatch.GetTimestamp();
                    InternalLogs.ExecuteException(_logger, taskUniqueId, ex);
                    status = InternalConstants.Failure;
                }

                InternalLogs.TaskCompleted(_logger, taskUniqueId, status, start == 0 ? 0 : InternalHelpers.GetElapsedMilliseconds(start, stop), null);

                IsExecuting = false;
            }
        }
    }
}