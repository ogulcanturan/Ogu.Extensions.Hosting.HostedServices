using System;

namespace Ogu.Extensions.Hosting.HostedServices
{
    internal class InternalConstants
    {
        public const string Success = "success";
        public const string Failure = "failure";

        public static TimeoutException TaskTimedOut { get; } = new TimeoutException("Task timed out");

        public static OperationCanceledException TaskCanceled { get; } = new OperationCanceledException("Task was canceled");
    }
}