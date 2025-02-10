using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Ogu.Extensions.Hosting.HostedServices;

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

        return new TimedHostedService(logger, "Logging", ExecuteAsync,
            opts =>
            {
                opts.StartsIn = TimeSpan.FromSeconds(5);
                opts.Period = TimeSpan.FromSeconds(5);
                opts.TaskTimeout = TimeSpan.FromSeconds(8);
            });

        ValueTask ExecuteAsync(CancellationToken c)
        {
            logger.LogInformation("************   Hey there! I'm working.   ************");

            return ValueTask.CompletedTask;
        }
    });
});

var host = hostBuilder.Build();

await host.RunAsync();