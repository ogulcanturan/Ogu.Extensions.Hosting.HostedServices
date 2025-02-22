using Microsoft.Extensions.Hosting;
using System;

namespace Ogu.Extensions.Hosting.HostedServices
{
    public interface ITimedHostedService : IHostedService
    {
        /// <summary>
        /// Updates the service options dynamically at runtime.
        /// </summary>
        void UpdateOptions(Action<TimedHostedServiceOptions> updateAction);
    }
}