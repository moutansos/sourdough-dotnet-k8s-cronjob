namespace CronJob.Template.Services;

public interface ICurrentTimeService
{
    DateTime CurrentTimeUtc { get; }
}

public class CurrentTimeService : ICurrentTimeService
{
    public DateTime CurrentTimeUtc => DateTime.UtcNow;
}

