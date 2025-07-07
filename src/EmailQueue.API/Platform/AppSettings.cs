using GaEpd.EmailService;
using System.Reflection;

namespace EmailQueue.API.Platform;

public static class AppSettings
{
    public static string? Version { get; internal set; }
    public static Queue QueueSettings { get; } = new();
    public static EmailServiceSettings EmailServiceSettings { get; } = new();
    public static Raygun RaygunSettings { get; } = new();

    public record Queue
    {
        public int ProcessingDelaySeconds { get; [UsedImplicitly] init; } = 5;
        // Default value of 5 seconds if not specified in config
    }

    public record Raygun
    {
        public string? ApiKey { get; [UsedImplicitly] init; }
    }
}

public static class AppSettingsExtensions
{
    public static void BindAppSettings(this WebApplicationBuilder builder)
    {
        AppSettings.Version = GetVersion();
        builder.Services.AddOptions<List<ApiClient>>().BindConfiguration(configSectionPath: "ApiClients");
        builder.Configuration.GetSection(nameof(AppSettings.QueueSettings))
            .Bind(AppSettings.QueueSettings);
        builder.Configuration.GetSection(nameof(AppSettings.EmailServiceSettings))
            .Bind(AppSettings.EmailServiceSettings);
        builder.Configuration.GetSection(nameof(AppSettings.RaygunSettings))
            .Bind(AppSettings.RaygunSettings);
    }

    private static string GetVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var segments = (entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? entryAssembly?.GetName().Version?.ToString() ?? "").Split('+');
        return segments[0] + (segments.Length > 0 ? $"+{segments[1][..Math.Min(7, segments[1].Length)]}" : "");
    }
}

public record ApiClient
{
    public required string ClientName { get; init; }
    public required Guid ClientId { get; init; }
    public required string ApiKey { get; init; }
}
