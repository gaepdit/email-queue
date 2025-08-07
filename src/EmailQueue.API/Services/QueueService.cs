using EmailQueue.API.Database;
using EmailQueue.API.Models;
using System.Collections.Concurrent;

namespace EmailQueue.API.Services;

public interface IQueueService
{
    Task<Guid> EnqueueEmailsAsync(NewEmailTask[] newEmailTasks, string clientName, Guid clientId);
    Task EnqueueEmailsForBatchAsync(Guid batchId, NewEmailTask[] newEmailTasks, string clientName, Guid clientId);
    Task<EmailTask?> DequeueAsync(CancellationToken cancellationToken);
    Task InitializeQueueFromDatabaseAsync();
}

public class QueueService(IServiceScopeFactory scopeFactory, ILogger<QueueService> logger) : IQueueService
{
    private readonly ConcurrentQueue<EmailTask> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private int _currentCounter;

    public async Task InitializeQueueFromDatabaseAsync()
    {
        List<EmailTask> pendingTasks;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pendingTasks = await db.GetQueuedItems();
            _currentCounter = await db.GetMaxCounter();
        }

        foreach (var task in pendingTasks)
        {
            _queue.Enqueue(task);
            _signal.Release();
        }

        logger.LogInformation("Initialized queue with {Count} pending tasks from database", pendingTasks.Count);
    }

    public async Task<Guid> EnqueueEmailsAsync(NewEmailTask[] newEmailTasks, string clientName, Guid clientId)
    {
        var batchId = Guid.NewGuid();
        await EnqueueEmailsForBatchAsync(batchId, newEmailTasks, clientName, clientId);
        return batchId;
    }

    public async Task EnqueueEmailsForBatchAsync(Guid batchId, NewEmailTask[] newEmailTasks, string clientName,
        Guid clientId)
    {
        if (newEmailTasks.Length == 0)
            throw new ArgumentException("Zero email tasks submitted", nameof(newEmailTasks));

        // Create new entities.
        var emailTasksList = newEmailTasks
            .Select(task => EmailTask.Create(task, batchId, clientName, clientId,
                counter: Interlocked.Increment(ref _currentCounter)))
            .ToArray();

        // Save new items to the database.
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.SaveBatchAsync(emailTasksList);
        }

        // Enqueue items in memory after they're saved to the database.
        foreach (var item in emailTasksList)
        {
            _queue.Enqueue(item);
            _signal.Release();
        }

        logger.LogInformation("Enqueued {Count} new email tasks", emailTasksList.Length);
    }

    public async Task<EmailTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _queue.TryDequeue(out var emailTask);
        return emailTask;
    }
}
