using System.Reflection;

namespace Sample.WebApp.Settings;

public static class AppSettings
{
    public static string? Version { get; private set; }
    public const string DateTimeFormat = "d\u2011MMM\u2011yyyy h:mm:ss\u00a0tt";

    public static void BindSettings(this WebApplicationBuilder builder)
    {
        Version = GetVersion();
        builder.Services.AddOptions<EmailQueueApi>().BindConfiguration(configSectionPath: nameof(EmailQueueApi));
    }

    private static string GetVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var segments = (entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? entryAssembly?.GetName().Version?.ToString() ?? "").Split('+');
        return segments[0] + (segments.Length > 0 ? $"+{segments[1][..Math.Min(7, segments[1].Length)]}" : "");
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public record EmailQueueApi
{
    public required string BaseUrl { get; init; }
    public required Guid ClientId { get; init; }
    public required string ApiKey { get; init; }
}
