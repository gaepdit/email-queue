using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Database;
using EmailQueue.API.Services;
using EmailQueue.API.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.Extensions.Logging;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Populate application settings.
builder.BindAppSettings();

// Configure data protection keys.
var dpb = builder.Services.AddDataProtection();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) dpb.ProtectKeysWithDpapi();

// Configure API controllers.
builder.Services.AddControllers();

// Configure the API Key authentication scheme.
builder.Services.AddAuthentication(nameof(SecuritySchemeType.ApiKey))
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(nameof(SecuritySchemeType.ApiKey), null);

// Add database context.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure email queue services.
builder.Services.AddSingleton<IQueueService, QueueService>();
builder.Services.AddHostedService<QueueBackgroundService>();
builder.Services.AddEmailServices();

// Configure application crash monitoring.
builder.Services.AddRaygun(builder.Configuration);
builder.Logging.AddRaygunLogger(options =>
{
    options.MinimumLogLevel = LogLevel.Warning;
    options.OnlyLogExceptions = false;
});

var app = builder.Build();


// Ensure the database is created.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}

app.UseRaygun();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok("OK"));
await app.RunAsync();
