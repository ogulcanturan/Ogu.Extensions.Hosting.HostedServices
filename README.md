# Ogu.Extensions.Hosting.HostedServices

[![.NET](https://github.com/ogulcanturan/Ogu.Extensions.Hosting.HostedServices/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/ogulcanturan/Ogu.Extensions.Hosting.HostedServices/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Ogu.Extensions.Hosting.HostedServices.svg?color=1ecf18)](https://nuget.org/packages/Ogu.Extensions.Hosting.HostedServices)
[![Nuget](https://img.shields.io/nuget/dt/Ogu.Extensions.Hosting.HostedServices.svg?logo=nuget)](https://nuget.org/packages/Ogu.Extensions.Hosting.HostedServices)

## Introduction

Ogu.Extensions.Hosting.HostedServices extends the `IHostedService` interface and provides built-in support for background task execution

## Features

- `TimedHostedService` class for running tasks with timed execution intervals.

## Installation

You can install the library via NuGet Package Manager:

```bash
dotnet add package Ogu.Extensions.Hosting.HostedServices
```

## Usage

**SettingsTimedHostedService:**
```csharp
public class SettingsTimedHostedService : TimedHostedService
{
    private readonly SettingsLocalService _service;

    public SettingsTimedHostedServices(ILogger<SettingsTimedHostedServices> logger, SettingsLocalService service, IOptions<SettingsConfiguration> settingsConfiguration) : base(logger, TimeSpan.Parse(settingsConfiguration.Value.Worker.Period), TimeSpan.Parse(settingsConfiguration.Value.Worker.StartsIn))
    {
        _service = service;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return _service.ReloadAsync(cancellationToken);
    }
}
```

**Registration:**
```csharp
services.AddHostedServices<SettingsTimedHostedService>();
```

Output =>

```bash
[2024-05-16T20:53:09.7608330Z-Dev-inf]: Ogu.Settings.Services.SettingsTimedHostedService
                                        Worker will start at: 2024-05-16T20:53:24.7574562Z and occur every 0:00:00:15.0000000 period. Maximum concurrently active jobs: 1
[2024-05-16T20:53:24.7864910Z-Dev-inf]: Ogu.Settings.Services.SettingsTimedHostedService
                                        Worker is executing the task
[2024-05-16T20:53:24.9255878Z-Dev-inf]: Ogu.Settings.Services.SettingsTimedHostedService
                                        Worker has executed the task in 134.811ms, next task at: 2024-05-16T20:53:39.9215909Z
```
