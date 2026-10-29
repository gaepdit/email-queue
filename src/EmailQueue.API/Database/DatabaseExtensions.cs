using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Database;

internal static class DatabaseExtensions
{
    public static void ConfigureDatabase(this WebApplicationBuilder builder)
    {
        if (builder.Configuration.GetValue<bool>("UseSqlServer"))
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection"),
                    opts => opts.EnableRetryOnFailure()));
        }
        else
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
        }
    }

    public static async Task BuildDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (app.Environment.IsDevelopment())
            await context.Database.EnsureCreatedAsync();
        else
            await context.Database.MigrateAsync();
    }
}
