using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ogu.Extensions.Hosting.HostedServices;

namespace TimedWorker;

public class CustomTimedService : TimedHostedService
{
    private readonly TimeSpan _masterStaleTimeout;

    public CustomTimedService(ILoggerFactory loggerFactory, IOptions<CustomTimedServiceOptions> customTimedServiceOpts) : base(loggerFactory.CreateLogger(nameof(CustomTimedService)), nameof(CustomTimedService), timedHostedServiceOpts => Configure(customTimedServiceOpts.Value, timedHostedServiceOpts))
    {
        var customTimedServiceOptions = customTimedServiceOpts.Value;

        _masterStaleTimeout = TimeSpan.FromMilliseconds(customTimedServiceOptions.MasterCheckInterval + customTimedServiceOptions.GraceBuffer);
    }

    protected override ValueTask DoWorkAsync(CancellationToken cancellationToken)
    {
        IsMasterStale(DateTime.UtcNow - TimeSpan.FromSeconds(Random.Shared.Next(1, 15)));

        return ValueTask.CompletedTask;
    }


    private bool IsMasterStale(DateTime lastHeartbeat)
    {
        var threshold = DateTime.UtcNow - _masterStaleTimeout;

        if (threshold > lastHeartbeat)
        {
            Logger.LogWarning("Master is stale. Last heartbeat: {lastHeartbeat}, Threshold: {threshold}", lastHeartbeat, threshold);

            return true;
        }

        Logger.LogDebug("Master is healthy. Last heartbeat: {lastHeartbeat}, Threshold: {threshold}", lastHeartbeat, threshold);

        return false;
    }

    private static void Configure(CustomTimedServiceOptions customTimedServiceOptions, TimedHostedServiceOptions timedHostedServiceOptions)
    {
        timedHostedServiceOptions.Period = TimeSpan.FromMilliseconds(customTimedServiceOptions.MasterCheckInterval);
        timedHostedServiceOptions.PreservePeriod = true;
    }
}