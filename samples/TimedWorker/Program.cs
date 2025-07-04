﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Ogu.Extensions.Hosting.HostedServices;
using TimedWorker;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.UseWindowsService(opts => opts.ServiceName = "TimedWorker");

hostBuilder.ConfigureServices(services =>
{
    services.AddLogging(cfg =>
    {
        cfg.AddSimpleConsole(opts =>
        {
            opts.TimestampFormat = "[dd-MM-yyyyTHH:mm:ss.fffffffK]-";
            opts.ColorBehavior = LoggerColorBehavior.Enabled;
        });
    });

    services.AddHostedService(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<TimedHostedService>>();

        return new TimedHostedService(logger, "TimedWorker", ExecuteAsync,
            opts =>
            {
                opts.StartsIn = TimeSpan.FromSeconds(5);
                opts.Period = TimeSpan.FromSeconds(5);
                opts.TaskTimeout = TimeSpan.FromSeconds(8);
                opts.LogOptions.LogWhenWorkerStartPlanned = true;
                opts.LogOptions.LogWhenTaskStarted = false;
                opts.LogOptions.LogWhenTaskCompleted = false;
                opts.LogOptions.LogWhenWorkerStopping = false;
                opts.LogOptions.LogWhenWorkerStopped = false;
                opts.LogOptions.LogWhenCaughtAnException = false;
                opts.LogOptions.LogWhenSkippingTask = false;
            });

        ValueTask ExecuteAsync(ITimedHostedService timedHostedService, CancellationToken c)
        {
            logger.LogInformation("************   Hey there! I'm working.   ************");

            return ValueTask.CompletedTask;
        }
    });

    services.AddHostedService<CustomTimedService>();
});

var host = hostBuilder.Build();

await host.RunAsync();