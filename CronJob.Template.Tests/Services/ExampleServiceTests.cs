using CronJob.Template.Infrastructure;
using CronJob.Template.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CronJob.Template.Tests.Services;

[TestClass]
public class ExampleServiceTests
{
    [TestMethod]
    public async Task TestMethod1() => await TestExampleService(
        setupDataLayerMock: dataLayerMock =>
        {
            // Setup dataLayerMock here if needed
        },
        setupLoggerMock: loggerMock =>
        {
            // Setup loggerMock here if needed
        },
        actions: async exampleService =>
        {

        });


    private static async Task TestExampleService(
        Func<IExampleService, Task> actions,
        Action<Mock<IExampleDataLayer>>? setupDataLayerMock = null,
        Action<Mock<ILogger<ExampleService>>>? setupLoggerMock = null)
    {
        Mock<IExampleDataLayer> dataLayerMock = new();
        Mock<ILogger<ExampleService>> loggerMock = new();

        setupDataLayerMock?.Invoke(dataLayerMock);
        setupLoggerMock?.Invoke(loggerMock);

        ExampleService exampleService = new(
            dataLayerMock.Object,
            loggerMock.Object);

        await actions(exampleService);
    }
}

