using CronJob.Template.Infrastructure;

namespace CronJob.Template.Services;

public interface IExampleService
{
}

public class ExampleService : IExampleService
{
    private readonly IExampleDataLayer dataLayer;
    private readonly ILogger<ExampleService> logger;

    public ExampleService(IExampleDataLayer dataLayer, ILogger<ExampleService> logger)
    {
        this.dataLayer = dataLayer;
        this.logger = logger;
    }
}

