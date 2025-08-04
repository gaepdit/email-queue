using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Database;

internal static class DbRepository
{
    public static async Task SaveBatchAsync(this AppDbContext db, params EmailTask[] emailTasksList)
    {
        await db.EmailTasks.AddRangeAsync(emailTasksList);
        await db.SaveChangesAsync();
    }

    public static async Task<List<BatchStatusView>> GetAllBatchesAsync(this AppDbContext db, Guid clientId) =>
        await QueryByClientId(db, clientId)
            .SelectBatchStatus()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

    public static async Task<List<EmailTaskStatusView>> GetBatchDetailsAsync(this AppDbContext db, Guid clientId,
        Guid batchId) =>
        await QueryByClientId(db, clientId)
            .Where(t => t.BatchId == batchId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new EmailTaskStatusView(t))
            .ToListAsync();

    public static async Task<BatchStatusView?> GetBatchStatusAsync(this AppDbContext db, Guid clientId, Guid batchId) =>
        await QueryByClientId(db, clientId)
            .Where(t => t.BatchId == batchId)
            .SelectBatchStatus()
            .SingleOrDefaultAsync();

    public static async Task<List<EmailTask>> GetQueuedItems(this AppDbContext dbContext) =>
        await dbContext.EmailTasks
            .Where(t => t.Status == nameof(EmailStatus.Queued))
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

    public static async Task<int> GetMaxCounter(this AppDbContext dbContext) =>
        await dbContext.EmailTasks.DefaultIfEmpty()
            .MaxAsync(t => t == null ? 0 : t.Counter);

    // Internal methods
    private static IQueryable<EmailTask> QueryByClientId(this AppDbContext db, Guid clientId) =>
        db.EmailTasks.Where(t => t.ClientId == clientId);

    private static IQueryable<BatchStatusView> SelectBatchStatus(this IQueryable<EmailTask> emailTasks) => emailTasks
        .GroupBy(t => t.BatchId)
        .Select(g => new BatchStatusView
        {
            BatchId = g.Key,
            Count = g.Count(),
            Queued = g.Count(t => t.Status == nameof(EmailStatus.Queued)),
            Sent = g.Count(t => t.Status == nameof(EmailStatus.Sent)),
            Failed = g.Count(t => t.Status == nameof(EmailStatus.Failed)),
            Skipped = g.Count(t => t.Status == nameof(EmailStatus.Skipped)),
            CreatedAt = g.Min(t => t.CreatedAt),
        });
}
