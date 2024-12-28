using Microsoft.Extensions.Logging.Console;
using Ogu.Extensions.Hosting.HostedServices;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(cfg =>
{
    cfg.AddSimpleConsole(opts =>
    {
        opts.TimestampFormat = "[dd-MM-yyyyTHH:mm:ss.fffffffK]-";
        opts.ColorBehavior = LoggerColorBehavior.Enabled;
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ITaskQueueFactory, TaskQueueFactory>();

builder.Services.AddHostedService(sp =>
{
    var taskQueueFactory = sp.GetRequiredService<ITaskQueueFactory>();

    var taskQueue = taskQueueFactory.GetOrCreate("my-queue", new BoundedChannelOptions(10));

    return new TaskQueueHostedService(sp.GetRequiredService<ILogger<TaskQueueHostedService>>(), taskQueue);
});

var app = builder.Build();

app.MapGet("/queues/names", (ITaskQueueFactory taskQueueFactory) => taskQueueFactory.GetQueueNames());

app.MapPost("/queues/{queueName}", async (string queueName, ILogger<Program> logger, ITaskQueueFactory taskQueueFactory, CancellationToken cancellationToken) =>
{
    var taskQueue = taskQueueFactory.GetOrCreate(queueName, new BoundedChannelOptions(10));

    await taskQueue.QueueTaskAsync((cancellation) =>
    {
        logger.LogInformation("Hey hey hey");

        return new ValueTask(Task.Delay(5000, cancellation));

    }, cancellationToken);

    return Results.Ok();
});

app.UseSwagger();
app.UseSwaggerUI();

app.Run();