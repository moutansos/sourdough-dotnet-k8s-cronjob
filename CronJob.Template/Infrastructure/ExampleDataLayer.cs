using CronJob.Template.Infrastructure.Commands;
using CronJob.Template.Infrastructure.Queries;

namespace CronJob.Template.Infrastructure;

public interface IExampleDataLayer
{
    Task<T> Execute<T>(IExampleDataLayerQuery<T> query);
    Task Execute(IExampleDataLayerCommand command);
    Task<T> Execute<T>(IExampleDataLayerCommand<T> command);
}

public class ExampleDataLayer(IExampleApiClient Client) : IExampleDataLayer
{
    public async Task<T> Execute<T>(IExampleDataLayerQuery<T> query) => await query.Execute(Client);
    public async Task Execute(IExampleDataLayerCommand command) => await command.Execute(Client);
    public async Task<T> Execute<T>(IExampleDataLayerCommand<T> command) => await command.Execute(Client);
}

