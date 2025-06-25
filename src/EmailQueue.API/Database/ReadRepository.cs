using EmailQueue.API.Models;
using EmailQueue.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Database;

internal static class ReadRepository
{
    public static async Task<List<BatchStatusView>> GetAllBatchesAsync(AppDbContext db, Guid clientId) =>
        await QueryByClientId(db, clientId)
            .SelectBatchStatus()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

    public static async Task<List<EmailTaskStatusView>> GetBatchDetailsAsync(AppDbContext db, Guid clientId,
        Guid batchId) =>
        await QueryByClientId(db, clientId)
            .Where(t => t.BatchId == batchId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new EmailTaskStatusView(t))
            .ToListAsync();

    public static async Task<BatchStatusView?> GetBatchStatusAsync(AppDbContext db, Guid clientId, Guid batchId) =>
        await QueryByClientId(db, clientId)
            .Where(t => t.BatchId == batchId)
            .SelectBatchStatus()
            .SingleOrDefaultAsync();

    private static IQueryable<EmailTask> QueryByClientId(AppDbContext db, Guid clientId) =>
        db.EmailTasks.Where(t => t.ClientId == clientId);

    private static IQueryable<BatchStatusView> SelectBatchStatus(this IQueryable<EmailTask> emailTasks) =>
        emailTasks.GroupBy(t => t.BatchId).Select(g =>
            new BatchStatusView
            {
                BatchId = g.Key,
                Count = g.Count(),
                Queued = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Queued)),
                Sent = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Sent)),
                Failed = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Failed)),
                Skipped = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Skipped)),
                CreatedAt = g.Min(t => t.CreatedAt),
            });
}
