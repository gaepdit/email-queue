using EmailQueue.Domain;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailQueue.SqliteMigrations;

// Facilitates some EF Core Tools commands. See "Design-time DbContext Creation":
// https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#from-a-design-time-factory

[UsedImplicitly]
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(options =>
            options.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.GetName().Name));
        return new AppDbContext(optionsBuilder.Options);
    }
}
