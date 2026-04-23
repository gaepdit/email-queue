using EmailQueue.API.Database;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
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
app.MapEndpoints();

await app.BuildDatabaseAsync();
await app.RunAsync();
