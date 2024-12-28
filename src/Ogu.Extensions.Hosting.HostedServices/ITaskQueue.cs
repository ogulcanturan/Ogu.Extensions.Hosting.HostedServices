using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.Extensions.Hosting.HostedServices
{
    public interface ITaskQueue
    {
        ValueTask QueueTaskAsync(Func<CancellationToken, ValueTask> task, CancellationToken cancellationToken = default);

        ValueTask<Func<CancellationToken, ValueTask>> DequeueTaskAsync(CancellationToken cancellationToken = default);
    }
}