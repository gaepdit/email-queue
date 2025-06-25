using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EmailTask> EmailTasks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<EmailTask>().HasKey(e => e.Id);
}
