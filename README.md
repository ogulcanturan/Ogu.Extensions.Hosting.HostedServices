# <img src="logo/ogu-logo.png" alt="Header" width="24"/> Ogu.Extensions.Hosting.HostedServices

[![.NET](https://github.com/ogulcanturan/Ogu.Extensions.Hosting.HostedServices/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/ogulcanturan/Ogu.Extensions.Hosting.HostedServices/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Ogu.Extensions.Hosting.HostedServices.svg?color=1ecf18)](https://nuget.org/packages/Ogu.Extensions.Hosting.HostedServices)
[![Nuget](https://img.shields.io/nuget/dt/Ogu.Extensions.Hosting.HostedServices.svg?logo=nuget)](https://nuget.org/packages/Ogu.Extensions.Hosting.HostedServices)

## Introduction

Ogu.Extensions.Hosting.HostedServices extends the `IHostedService` interface and provides built-in support for background task execution

## Features

- `TimedHostedService` class for running tasks with timed execution intervals.

- `QueueHostedService` class for running tasks by processing items from a queue asynchronously as they are added.

## Installation

You can install the library via NuGet Package Manager:

```bash
dotnet add package Ogu.Extensions.Hosting.HostedServices
```

## TimedHostedService Usage 

**Registration:**
```csharp
services.AddHostedService(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TimedHostedService>>();

    return new TimedHostedService(logger, "TimedWorker", ExecuteAsync,
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
```

Output =>

```bash
[28-12-2024T22:52:15.5558831+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TimedHostedService[1]
      TimedWorker is scheduled to start at:2024-12-28T21:52:20.5499759Z and occur every 0:00:00:05.0000000 period.
[28-12-2024T22:52:20.6032080+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TimedHostedService[3]
      TimedWorker task @T-1735422740597-@Thread-5 started.
[28-12-2024T22:52:20.6083551+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TimedHostedService[0]
      ************   Hey there! I'm working.   ************
[28-12-2024T22:52:20.6165092+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TimedHostedService[4]
      TimedWorker task @T-1735422740597-@Thread-5 completed with status: success in 4.7972ms, next task at: 2024-12-28T21:52:25.5959966Z.
```

## QueueHostedService Usage

**Registration:**
```csharp
builder.Services.AddSingleton<ITaskQueueFactory, TaskQueueFactory>();

builder.Services.AddHostedService(sp =>
{
    var taskQueueFactory = sp.GetRequiredService<ITaskQueueFactory>();

    var taskQueue = taskQueueFactory.GetOrCreate("my-queue", "QueueWorker", new BoundedChannelOptions(10));

    return new TaskQueueHostedService(sp.GetRequiredService<ILogger<TaskQueueHostedService>>(), taskQueue);
});
```

**Basic usage:**

```csharp
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
```

Output =>

```bash
[28-12-2024T23:14:14.2906716+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TaskQueueHostedService[1]
      QueueWorker started.
[28-12-2024T23:14:24.8109179+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TaskQueueHostedService[3]
      QueueWorker task @T-1735424054321-@Thread-1 started.
[28-12-2024T23:14:24.8132472+01:00]-info: Program[0]
      Hey hey hey
[28-12-2024T23:14:29.8306265+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TaskQueueHostedService[4]
      QueueWorker task @T-1735424054321-@Thread-1 completed with status: success in 5007.4033ms.
[28-12-2024T23:14:29.8398904+01:00]-info: Ogu.Extensions.Hosting.HostedServices.TaskQueueHostedService[3]
      QueueWorker task @T-1735424069838-@Thread-16 started.
[28-12-2024T23:14:29.8465989+01:00]-info: Program[0]
      Hey hey hey
```

## Sample Application

A sample application demonstrating the usage of **TimedHostedService** can be found [here](https://github.com/ogulcanturan/Ogu.Extensions.Hosting.HostedServices/tree/master/samples/TimedWorker).

A sample application demonstrating the usage of **QueueHostedService** can be found [here](https://github.com/ogulcanturan/Ogu.Extensions.Hosting.HostedServices/tree/master/samples/QueueWorker).
