using Microsoft.Extensions.Logging;
using System;

namespace Ogu.Extensions.Hosting.HostedServices
{
    internal static class InternalLogs
    {
        private static class EventIds
        {
            public static readonly EventId WorkerStarted = new EventId(1, "WorkerStarted");
            public static readonly EventId WorkerStartPlanned = new EventId(2, "WorkerStartPlanned");
            public static readonly EventId TaskStarted = new EventId(3, "TaskStarted");
            public static readonly EventId TaskCompleted = new EventId(4, "TaskCompleted");
            public static readonly EventId TaskCompletedWithNext = new EventId(5, "TaskCompletedWithNext");
            public static readonly EventId WorkerStopping = new EventId(6, "WorkerStopping");
            public static readonly EventId WorkerStopped = new EventId(7, "WorkerStopped");
            public static readonly EventId ExecuteException = new EventId(8, "ExecuteException");
            public static readonly EventId SkippingTask = new EventId(9, "SkippingTask");
            public static readonly EventId WorkerHasAlreadyStarted = new EventId(10, "WorkerHasAlreadyStarted");
            public static readonly EventId StartingDisposedWorker = new EventId(11, "StartingDisposedWorker");
            public static readonly EventId StoppingDisposedWorker = new EventId(11, "StoppingDisposedWorker");
        }

        public static readonly Action<ILogger, string, Exception> WorkerStarted = LoggerMessage.Define<string>(
            LogLevel.Information,
            EventIds.WorkerStarted,
            "{worker} started.");

        public static readonly Action<ILogger, string, DateTime, TimeSpan, Exception> WorkerStartPlanned = LoggerMessage.Define<string, DateTime, TimeSpan>(
            LogLevel.Information,
            EventIds.WorkerStartPlanned,
            "{worker} is scheduled to start at:{dateTime:o} and occur every {period:G} period.");

        public static readonly Action<ILogger, string, string, Exception> TaskStarted = LoggerMessage.Define<string, string>(
            LogLevel.Information,
            EventIds.TaskStarted,
            "{worker} task {taskUniqueId} started.");

        public static readonly Action<ILogger, string, string, string, double, Exception> TaskCompleted = LoggerMessage.Define<string, string, string, double>(
            LogLevel.Information,
            EventIds.TaskCompleted,
            "{worker} task {taskUniqueId} completed with status: {status} in {elapsed}ms.");

        public static readonly Action<ILogger, string, string, string, double, DateTime, Exception> TaskCompletedWithNext = LoggerMessage.Define<string, string, string, double, DateTime>(
            LogLevel.Information,
            EventIds.TaskCompletedWithNext,
            "{worker} task {taskUniqueId} completed with status: {status} in {elapsed}ms, next task at: {nextTaskAt:o}.");

        public static readonly Action<ILogger, string, Exception> WorkerStopping = LoggerMessage.Define<string>(
            LogLevel.Information,
            EventIds.WorkerStopping,
            "{worker} is stopping...........");

        public static readonly Action<ILogger, string, Exception> WorkerStopped = LoggerMessage.Define<string>(
            LogLevel.Information,
            EventIds.WorkerStopped,
            "{worker} stopped.");

        public static readonly Action<ILogger, string, string, Exception> CaughtAnException = LoggerMessage.Define<string, string>(
            LogLevel.Error,
            EventIds.ExecuteException,
            "{worker} caught an exception while executing the task {taskUniqueId}");

        public static readonly Action<ILogger, string, Exception> SkippingTask = LoggerMessage.Define<string>(
            LogLevel.Information,
            EventIds.SkippingTask,
            "{worker} skipping task execution because another task is currently running.");

        public static readonly Action<ILogger, string, Exception> WorkerHasAlreadyStarted =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.WorkerHasAlreadyStarted,
                "{worker} has already started.");

        public static readonly Action<ILogger, string, Exception> StartingDisposedWorker =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.StartingDisposedWorker,
                "{worker} cannot be started as it is disposed.");

        public static readonly Action<ILogger, string, Exception> StoppingDisposedWorker =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.StoppingDisposedWorker,
                "{worker} cannot be stopped as it is disposed.");
    }
}
