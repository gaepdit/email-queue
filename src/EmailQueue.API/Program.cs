using EmailQueue.API.Database;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
using Microsoft.AspNetCore.Authorization;
using Mindscape.Raygun4Net.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddZLoggerConsole(options =>
{
    if (builder.Environment.IsDevelopment())
        options.UsePlainTextFormatter();
    else
        options.UseJsonFormatter();
});
builder.BindAppSettings();
builder.ConfigureRaygunLogging();
builder.Services.AddControllers();
builder.Services.AddApiKeyAuthentication();
builder.ConfigureDatabase();
builder.Services.AddEmailQueueServices();

var app = builder.Build();

if (!string.IsNullOrEmpty(AppSettings.RaygunSettings.ApiKey)) app.UseRaygun();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.BuildDatabaseAsync();

app.MapGet("/health", () => Results.Ok("OK"));
app.MapGet("/version", () => Results.Ok(new { AppSettings.Version }));
app.MapGet("/check-api-auth", CheckApiAuth);

await app.RunAsync();
return;

[Authorize(AuthenticationSchemes = nameof(ApiKeyAuthenticationHandler))]
static IResult CheckApiAuth() => Results.Ok("Auth OK");
