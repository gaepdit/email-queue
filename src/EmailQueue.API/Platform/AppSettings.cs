using GaEpd.EmailService;

namespace EmailQueue.API.Platform;

public static class AppSettings
{
    public static QueueSettings QueueSettings { get; } = new();
    public static EmailServiceSettings EmailServiceSettings { get; } = new();
}

public static class AppSettingsExtensions
{
    public static void BindAppSettings(this WebApplicationBuilder builder)
    {
        // Bind app settings.
        builder.Services.AddOptions<List<ApiClient>>().BindConfiguration(configSectionPath: "ApiClients");
        builder.Configuration.GetSection(nameof(AppSettings.QueueSettings)).Bind(AppSettings.QueueSettings);
        builder.Configuration.GetSection(nameof(AppSettings.EmailServiceSettings))
            .Bind(AppSettings.EmailServiceSettings);
    }
}

public record QueueSettings
{
    public int ProcessingDelaySeconds { get; [UsedImplicitly] init; } = 5; // Default value if not specified in config
}

public record ApiClient
{
    public required string ClientName { get; init; }
    public required Guid ClientId { get; init; }
    public required string ApiKey { get; init; }
}
