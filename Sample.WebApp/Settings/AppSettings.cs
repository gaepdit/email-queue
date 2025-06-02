using System.Reflection;

namespace Sample.WebApp.Settings;

public static class AppSettings
{
    public static string? InformationalVersion { get; private set; }
    public static string? InformationalBuild { get; private set; }
    public const string DateTimeFormat = "d\u2011MMM\u2011yyyy h:mm:ss\u00a0tt";

    public static void BindSettings(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<EmailQueueApi>().BindConfiguration(configSectionPath: nameof(EmailQueueApi));

        // App version
        var segments = (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "").Split('+');
        InformationalVersion = segments[0];
        if (segments.Length > 0 && segments[1].Length > 0)
            InformationalBuild = segments[1][..Math.Min(7, segments[1].Length)];
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public record EmailQueueApi
{
    public required string BaseUrl { get; init; }
    public required Guid ClientId { get; init; }
    public required string ApiKey { get; init; }
}
