namespace CronJob.Template.BackgroundServices;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    //Your main service here

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        this.logger = logger;
        this.hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Staring Service....");

            //await main service logic call here
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occured in cron job");
        }
        finally
        {
            logger.LogInformation("Finished Run.");
            Thread.Sleep(5000); //Give some time for logs to be sent
            hostApplicationLifetime.StopApplication();
        }
    }

}
