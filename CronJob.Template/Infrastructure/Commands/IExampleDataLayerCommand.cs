namespace CronJob.Template.Infrastructure.Commands;

public interface IExampleDataLayerCommand
{
    Task Execute(IExampleApiClient client);
}

public interface IExampleDataLayerCommand<T>
{
    Task<T> Execute(IExampleApiClient client);
}

