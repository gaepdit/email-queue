using EmailQueue.API.Database;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
using Mindscape.Raygun4Net.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.BindAppSettings();
builder.ConfigureRaygunLogging();
builder.Services.AddControllers();
builder.Services.AddApiKeyAuthentication();
builder.ConfigureDatabase();
builder.Services.AddEmailQueueServices();

var app = builder.Build();
app.UseRaygun();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));
await app.BuildDatabaseAsync();

await app.RunAsync();
