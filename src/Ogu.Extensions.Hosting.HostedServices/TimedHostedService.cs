using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.Extensions.Hosting.HostedServices
{
    public abstract class TimedHostedService : IHostedService, IDisposable
    {
        private int _numberOfActiveJobs;
        private Timer _timer;
        private Task _executingTask;
        private CancellationTokenSource _stoppingCts;
        private readonly TimeSpan _startsIn;
        private bool _disposed;
        private readonly int _maximumActiveJobs;

        protected readonly ILogger Logger;
        protected TimeSpan Period;

        protected TimedHostedService(ILogger logger, TimeSpan period, TimeSpan startsIn = default, int maximumActiveJobs = 1)
        {
            Logger = logger ?? new NullLogger<TimedHostedService>();
            Period = period;
            _startsIn = startsIn;
            _maximumActiveJobs = maximumActiveJobs;
        }

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        public virtual bool IsExecuting { get; private set; }
        public virtual bool HasStarted { get; private set; }
        public virtual DateTime? NextTaskAt { get; private set; }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            HasStarted = true;

            _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Log.WorkerStarted(Logger, DateTime.UtcNow.Add(_startsIn), Period, _maximumActiveJobs, null);

            _timer = new Timer(x => _executingTask = DoWorkAsync(x, _stoppingCts.Token), null, _startsIn, Period);

            return Task.CompletedTask;
        }

        // Ref: https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs
        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            Log.WorkerStopped(Logger, null);

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
#if NETSTANDARD2_0
                using (var registration = cancellationToken.Register(s => ((TaskCompletionSource<object>)s).SetCanceled(), tcs))
                {
                    // Do not await the _executeTask because cancelling it will throw an OperationCanceledException which we are explicitly ignoring
                    await Task.WhenAny(_executingTask, tcs.Task).ConfigureAwait(false);
                }
#else
                await using (var registration = cancellationToken.Register(s => ((TaskCompletionSource<object>)s).SetCanceled(), tcs))
                {
                    // Do not await the _executeTask because cancelling it will throw an OperationCanceledException which we are explicitly ignoring
                    await Task.WhenAny(_executingTask, tcs.Task).ConfigureAwait(false);
                }
#endif
#endif
                HasStarted = false;
                NextTaskAt = null;
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
            if (_numberOfActiveJobs < _maximumActiveJobs)
            {
                _numberOfActiveJobs++;

                Log.ExecutingTheTask(Logger, null);

                IsExecuting = true;

                var start = Stopwatch.GetTimestamp();
                long stop;

                try
                {
                    await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                    stop = Stopwatch.GetTimestamp();
                }
                catch (Exception ex)
                {
                    stop = Stopwatch.GetTimestamp();
                    Log.ExecuteException(Logger, ex);
                }
                finally
                {
                    _numberOfActiveJobs--;
                }

                NextTaskAt = DateTime.UtcNow.Add(Period);

                Log.ExecutedTheTask(Logger, GetElapsedMilliseconds(start, stop), NextTaskAt.Value, null);

                IsExecuting = false;
            }
        }

        private static double GetElapsedMilliseconds(long start, long stop) => (stop - start) * 1000 / (double)Stopwatch.Frequency;

        private static class Log
        {
            private static class EventIds
            {
                public static readonly EventId WorkerStarted = new EventId(1, "WorkerStarted");
                public static readonly EventId ExecutingTheTask = new EventId(2, "ExecutingTheTask");
                public static readonly EventId ExecutedTheTask = new EventId(3, "ExecutedTheTask");
                public static readonly EventId WorkerStopped = new EventId(4, "WorkerStopped");
                public static readonly EventId ExecuteException = new EventId(5, "ExecuteException");
            }

            public static readonly Action<ILogger, DateTime, TimeSpan, int, Exception> WorkerStarted = LoggerMessage.Define<DateTime, TimeSpan, int>(
                LogLevel.Information,
                EventIds.WorkerStarted,
                "Worker will start at: {DateTime:o} and occur every {Period:G} period. Maximum concurrently active jobs: {MaximumActiveJobs}");

            public static readonly Action<ILogger, Exception> ExecutingTheTask = LoggerMessage.Define(
                LogLevel.Information,
                EventIds.ExecutingTheTask,
                "Worker is executing the task");

            public static readonly Action<ILogger, double, DateTime, Exception> ExecutedTheTask = LoggerMessage.Define<double, DateTime>(
                LogLevel.Information,
                EventIds.ExecutedTheTask,
                "Worker has executed the task in {Elapsed}ms, next task at: {DateTime:o}");

            public static readonly Action<ILogger, Exception> WorkerStopped = LoggerMessage.Define(
                LogLevel.Information,
                EventIds.WorkerStopped,
                "Worker is stopping...........");

            public static readonly Action<ILogger, Exception> ExecuteException = LoggerMessage.Define(
                LogLevel.Error,
                EventIds.ExecuteException,
                "Worker caught an exception while executing the task!");
        }
    }
}