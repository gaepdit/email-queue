using EmailQueue.API.Database;
using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EmailQueue.API.Services;

public interface IQueueService
{
    Task<Guid?> EnqueueItems(NewEmailTask[] newEmailTasks, string clientName, Guid clientId);
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
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

    public async Task<Guid?> EnqueueItems(NewEmailTask[] newEmailTasks, string clientName, Guid clientId)
    {
        if (newEmailTasks.Length == 0) return null;

        // Create new entities.
        var batchId = Guid.NewGuid();
        var emailTasksList = newEmailTasks
            .Select(task => EmailTask.Create(task, batchId, clientName, clientId,
                counter: Interlocked.Increment(ref _currentCounter)))
            .ToList();

        // Save new items to the database.
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
