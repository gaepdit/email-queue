using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Database;

internal static class DbRepository
{
    extension(AppDbContext db)
    {
        public async Task SaveBatchAsync(params EmailTask[] emailTasksList)
        {
            await db.EmailTasks.AddRangeAsync(emailTasksList);
            await db.SaveChangesAsync();
        }

        public async Task<List<BatchStatusView>> GetAllBatchesAsync(Guid clientId) =>
            await QueryByClientId(db, clientId)
                .SelectBatchStatus()
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

        public async Task<List<EmailTaskStatusView>> GetBatchDetailsAsync(Guid clientId, Guid batchId) =>
            await QueryByClientId(db, clientId)
                .Where(t => t.BatchId == batchId)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new EmailTaskStatusView(t))
                .ToListAsync();

        public async Task<List<EmailTaskStatusView>> GetBatchFailedItemsAsync(Guid clientId, Guid batchId) =>
            await QueryByClientId(db, clientId)
                .Where(t =>
                    t.BatchId == batchId &&
                    (t.Status == nameof(EmailStatus.Failed) || t.Status == nameof(EmailStatus.Skipped)))
                .OrderBy(t => t.CreatedAt)
                .Select(t => new EmailTaskStatusView(t))
                .ToListAsync();

        public async Task<BatchStatusView?> GetBatchStatusAsync(Guid clientId, Guid batchId) =>
            await QueryByClientId(db, clientId)
                .Where(t => t.BatchId == batchId)
                .SelectBatchStatus()
                .SingleOrDefaultAsync();

        public async Task<List<EmailTask>> GetQueuedItems() =>
            await db.EmailTasks
                .Where(t => t.Status == nameof(EmailStatus.Queued))
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

        public async Task<int> GetMaxCounter() =>
            await db.EmailTasks.DefaultIfEmpty()
                .MaxAsync(t => t == null ? 0 : t.Counter);

        private IQueryable<EmailTask> QueryByClientId(Guid clientId) =>
            db.EmailTasks.Where(t => t.ClientId == clientId);
    }

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
