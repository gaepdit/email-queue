using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Data;
using EmailQueue.API.Services;
using EmailQueue.API.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Populate application settings.
builder.BindAppSettings();

// Persist data protection keys.
await builder.AddDataProtectionServices();

// Configure API controllers.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
    {
        const string securityDefinitionName = "ApiKeyHeader";
        options.AddSecurityDefinition(securityDefinitionName, new OpenApiSecurityScheme()
        {
            Name = ApiKeyAuthenticationHandler.ApiKeyHeaderName,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
        });
        var openApiSecurityScheme = new OpenApiSecurityScheme
            { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = securityDefinitionName } };
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { { openApiSecurityScheme, [] } });
    });
}

// Configure the API Key authentication scheme.
builder.Services.AddAuthentication(nameof(SecuritySchemeType.ApiKey))
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(nameof(SecuritySchemeType.ApiKey), null);

// Configure authorization policies.
builder.Services.AddAuthorizationPolicies();

// Add database context.
builder.Services.AddDbContext<EmailQueueDbContext>(options =>
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
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EmailQueueDbContext>();
    await context.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRaygun();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok());
await app.RunAsync();
