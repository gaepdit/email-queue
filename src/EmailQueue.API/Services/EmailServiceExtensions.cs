using GaEpd.EmailService.Utilities;

namespace EmailQueue.API.Services;

public static class EmailServiceExtensions
{
    public static void AddEmailQueueServices(this IServiceCollection services)
    {
        services.AddSingleton<IQueueService, QueueService>();
        services.AddHostedService<QueueBackgroundService>();
        services.AddEmailService();
        services.AddScoped<IEmailProcessorService, EmailProcessorService>();
    }
}
