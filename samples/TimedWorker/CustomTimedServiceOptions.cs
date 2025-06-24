using Microsoft.Extensions.Options;

namespace TimedWorker;

public class CustomTimedServiceOptions : IOptions<CustomTimedServiceOptions>
{
    private int _masterCheckInterval = 8000;

    public int MasterCheckInterval
    {
        get => _masterCheckInterval;
        set
        {
            _masterCheckInterval = value;
            CalculateHeartbeatInterval();
        }
    }

    public int HeartbeatInterval { get; private set; } = 5000;

    public int GraceBuffer { get; set; } = 1000;

    private void CalculateHeartbeatInterval()
    {
        HeartbeatInterval = _masterCheckInterval / 2 + 1;
    }

    CustomTimedServiceOptions IOptions<CustomTimedServiceOptions>.Value => this;
}