namespace CronJob.Template.Infrastructure.Queries;

public interface IExampleDataLayerQuery<T>
{
    Task<T> Execute(IExampleApiClient client);
}

