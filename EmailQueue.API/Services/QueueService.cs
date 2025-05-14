using EmailQueue.API.Data;
using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EmailQueue.API.Services;

public interface IQueueService
{
    Task<string?> EnqueueItems(NewEmailTask[] newEmailTasks, string client, Guid clientId);
    Task<EmailTask?> DequeueAsync(CancellationToken cancellationToken);
    Task InitializeQueueFromDatabase();
}

public class QueueService(IServiceScopeFactory scopeFactory, ILogger<QueueService> logger) : IQueueService
{
    private readonly ConcurrentQueue<EmailTask> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private int _currentCounter;

    public async Task InitializeQueueFromDatabase()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmailQueueDbContext>();

        var pendingTasks = await dbContext.EmailTasks
            .Where(t => t.Status == "Queued")
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        foreach (var task in pendingTasks)
        {
            _queue.Enqueue(task);
            _signal.Release();
        }

        _currentCounter = await dbContext.EmailTasks.DefaultIfEmpty().MaxAsync(t => t == null ? 0 : t.Counter);
        logger.LogInformation("Initialized queue with {Count} pending tasks from database", pendingTasks.Count);
    }

    private const string Pool = "ABCDEFGHKMNPQRSTUVWXYZ2345689";
    private static string GetBatchId() => new(Random.Shared.GetItems<char>(Pool, 10));

    public async Task<string?> EnqueueItems(NewEmailTask[] newEmailTasks, string client, Guid clientId)
    {
        if (newEmailTasks.Length == 0) return null;

        // Create new entities.
        var batchId = GetBatchId();
        var emailTasksList = newEmailTasks
            .Select(task => EmailTask.Create(task, batchId, client, clientId,
                counter: Interlocked.Increment(ref _currentCounter)))
            .ToList();

        // Save new items to the database.
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmailQueueDbContext>();
        await dbContext.EmailTasks.AddRangeAsync(emailTasksList);
        await dbContext.SaveChangesAsync();

        // Enqueue items in memory after they're saved to the database.
        foreach (var item in emailTasksList)
        {
            _queue.Enqueue(item);
            _signal.Release();
        }

        logger.LogInformation("Enqueued {Count} new email tasks", emailTasksList.Count);
        return batchId;
    }

    public async Task<EmailTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _queue.TryDequeue(out var emailTask);
        return emailTask;
    }
}
