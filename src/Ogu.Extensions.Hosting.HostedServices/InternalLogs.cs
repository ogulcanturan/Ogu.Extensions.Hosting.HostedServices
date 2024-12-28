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
        }

        public static readonly Action<ILogger, Exception> WorkerStarted = LoggerMessage.Define(
            LogLevel.Information,
            EventIds.WorkerStarted,
            "Worker started.");

        public static readonly Action<ILogger, DateTime, TimeSpan, Exception> WorkerStartPlanned = LoggerMessage.Define<DateTime, TimeSpan>(
            LogLevel.Information,
            EventIds.WorkerStarted,
            "Worker is scheduled to start at:{DateTime:o} and occur every {Period:G} period.");

        public static readonly Action<ILogger, string, Exception> TaskStarted = LoggerMessage.Define<string>(
            LogLevel.Information,
            EventIds.TaskStarted,
            "Task {TaskUniqueId} started.");

        public static readonly Action<ILogger, string, string, double, Exception> TaskCompleted = LoggerMessage.Define<string, string, double>(
            LogLevel.Information,
            EventIds.TaskCompleted,
            "Task {TaskUniqueId} completed with status: {Status} in {Elapsed}ms.");

        public static readonly Action<ILogger, string, string, double, DateTime, Exception> TaskCompletedWithNext = LoggerMessage.Define<string, string, double, DateTime>(
            LogLevel.Information,
            EventIds.TaskCompleted,
            "Task {TaskUniqueId} completed with status: {Status} in {Elapsed}ms, next task at: {NextTaskAt:o}.");

        public static readonly Action<ILogger, Exception> WorkerStopping = LoggerMessage.Define(
            LogLevel.Information,
            EventIds.WorkerStopped,
            "Worker is stopping...........");

        public static readonly Action<ILogger, Exception> WorkerStopped = LoggerMessage.Define(
            LogLevel.Information,
            EventIds.WorkerStopped,
            "Worker stopped.");

        public static readonly Action<ILogger, string, Exception> ExecuteException = LoggerMessage.Define<string>(
            LogLevel.Error,
            EventIds.ExecuteException,
            "Worker caught an exception while executing the task {TaskUniqueId}");

        public static readonly Action<ILogger, Exception> SkippingTask = LoggerMessage.Define(
            LogLevel.Information,
            EventIds.SkippingTask,
            "Skipping task execution because another task is currently running.");
    }
}
