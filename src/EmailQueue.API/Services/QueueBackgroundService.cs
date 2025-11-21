using EmailQueue.API.Platform;

namespace EmailQueue.API.Services;

public class QueueBackgroundService(
    IQueueService queueService,
    ILogger<QueueBackgroundService> logger,
    IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize the queue with pending tasks from the database.
        await queueService.InitializeQueueFromDatabaseAsync();
        logger.ZLogInformation($"DataProcessorService started and queue initialized.");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var emailTask = await queueService.DequeueAsync(stoppingToken);
                if (emailTask == null) continue;

                using (var scope = scopeFactory.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailProcessorService>();
                    await emailService.ProcessEmailAsync(emailTask);
                }

                logger.ZLogInformation(
                    $"Waiting {AppSettings.QueueSettings.ProcessingDelaySeconds:@Delay} seconds before processing next task");
                await Task.Delay(TimeSpan.FromSeconds(AppSettings.QueueSettings.ProcessingDelaySeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                var counter = ex.Data["Counter"]?.ToString() ?? "Unknown Counter";
                var id = ex.Data["Id"]?.ToString() ?? "Unknown Id";
                logger.ZLogError(ex, $"Failed to send email task {counter} ({id})");
            }
        }
    }
}
