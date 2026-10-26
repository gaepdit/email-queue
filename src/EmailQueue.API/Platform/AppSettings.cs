using Azure.Identity;
using GaEpd.EmailService;
using System.Reflection;

namespace EmailQueue.API.Platform;

internal static class AppSettings
{
    public static string? Version { get; private set; }
    public static Queue QueueSettings { get; } = new();
    public static EmailServiceSettings EmailServiceSettings { get; } = new();

    public record Queue
    {
        public int ProcessingDelaySeconds { get; [UsedImplicitly] init; } = 5;
        // Default value of 5 seconds if not specified in config
    }

    public static void BindAppSettings(this WebApplicationBuilder builder)
    {
        Version = GetVersion();
        builder.AddKeyVault();

        builder.Services.AddOptions<List<ApiClient>>().BindConfiguration(configSectionPath: "ApiClients");
        builder.Configuration.GetSection(nameof(QueueSettings))
            .Bind(QueueSettings);
        builder.Configuration.GetSection(nameof(EmailServiceSettings))
            .Bind(EmailServiceSettings);
    }

    private static string GetVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var segments = (entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? entryAssembly?.GetName().Version?.ToString() ?? "").Split('+');
        return segments[0] + (segments.Length > 0 ? $"+{segments[1][..Math.Min(7, segments[1].Length)]}" : "");
    }

    private static void AddKeyVault(this IHostApplicationBuilder builder)
    {
        var keyVaultName = builder.Configuration.GetValue<string>("AzureKeyVaultName");
        if (string.IsNullOrEmpty(keyVaultName)) return;

        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}

public record ApiClient
{
    public required string ClientName { get; init; }
    public required Guid ClientId { get; init; }
    public required string ApiKey { get; init; }
}
