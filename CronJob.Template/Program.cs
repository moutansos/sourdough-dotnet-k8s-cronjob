using CronJob.Template.BackgroundServices;
using CronJob.Template.Infrastructure;
using CronJob.Template.Services;
using dotenv.net;
using SlackLogger;

DotEnv.Load();

IHostBuilder builder = Host.CreateDefaultBuilder(args);

builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole()
        .AddSlack(options =>
        {
            #pragma warning disable CS0618, CS0612 // Disable specific obsolete warnings
            options.UserName = null;
            #pragma warning restore CS0618, CS0612 // Restore specific obsolete warnings

            options.WebhookUrl = Environment.GetEnvironmentVariable(Constants.SLACK_LOGGER_WEBHOOK_URL) ?? 
                throw new ArgumentNullException(Constants.SLACK_LOGGER_WEBHOOK_URL, $"{Constants.SLACK_LOGGER_WEBHOOK_URL} environment variable is not set");
            options.LogLevel = LogLevel.Warning;
            options.SanitizeOutputFunction = (message) => message.Substring(0, Math.Min(message.Length, 1000));
        });
});

builder.ConfigureServices((context, services) =>
{
    services.AddHostedService<Worker>();

    services.AddHttpClient<IExampleApiClient, ExampleApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com/");
    });

    services.AddTransient<IExampleDataLayer, ExampleDataLayer>();
    services.AddTransient<ICurrentTimeService, CurrentTimeService>();
    services.AddTransient<IExampleService, ExampleService>();
    // TODO: Register other services here
});

IHost host = builder.Build();
await host.RunAsync();
