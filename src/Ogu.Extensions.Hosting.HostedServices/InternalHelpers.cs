using System;
using System.Diagnostics;
using System.Threading;

namespace Ogu.Extensions.Hosting.HostedServices
{
    internal class InternalHelpers
    {
        public static string GetTaskUniqueId()
        {
            return $"@T-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-@Thread-{Thread.CurrentThread.ManagedThreadId}";
        }

        public static double GetElapsedMilliseconds(long start, long stop) => (stop - start) * 1000 / (double)Stopwatch.Frequency;
    }
}
