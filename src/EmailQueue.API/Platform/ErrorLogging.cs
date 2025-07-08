using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.Extensions.Logging;

namespace EmailQueue.API.Platform;

internal static class ErrorLogging
{
    public static void ConfigureRaygunLogging(this WebApplicationBuilder builder)
    {
        if (string.IsNullOrEmpty(AppSettings.RaygunSettings.ApiKey)) return;

        builder.Services.AddRaygun(options =>
        {
            options.ApiKey = AppSettings.RaygunSettings.ApiKey;
            options.ApplicationVersion = AppSettings.Version;
        });
        builder.Logging.AddRaygunLogger(options =>
        {
            options.MinimumLogLevel = LogLevel.Warning;
            options.OnlyLogExceptions = false;
        });
    }
}
