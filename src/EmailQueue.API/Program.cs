using EmailQueue.API.Database;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddZLoggerConsole(options => options.UseJsonFormatter());
builder.BindAppSettings();
builder.Services.AddControllers();
builder.Services.AddApiKeyAuthentication();
builder.ConfigureDatabase();
builder.Services.AddEmailQueueServices();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapEndpoints();

await app.BuildDatabaseAsync();
await app.RunAsync();
