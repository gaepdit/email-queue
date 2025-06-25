using EmailQueue.API.Platform;

namespace EmailQueue.API.Models;

public record EmailTask : NewEmailTask
{
    // Constructors
    [UsedImplicitly]
    private EmailTask() { } // Used by ORM.

    private EmailTask(Guid id) => Id = id;

    // Properties
    public Guid Id { get; }
    public Guid BatchId { get; private init; }
    public int Counter { get; private init; }

    [StringLength(50)]
    public string? ClientName { get; private init; }

    public Guid ClientId { get; private init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(15)]
    public string Status { get; private set; } = nameof(EmailStatus.Queued);

    [StringLength(200)]
    public string? FailureReason { get; private set; }

    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? AttemptedAt { get; private set; }

    // Methods
    public void MarkAsSent()
    {
        Status = nameof(EmailStatus.Sent);
        AttemptedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? reason)
    {
        Status = nameof(EmailStatus.Failed);
        AttemptedAt = DateTime.UtcNow;
        FailureReason = reason.Truncate(200);
    }

    public void MarkAsSkipped()
    {
        Status = nameof(EmailStatus.Skipped);
        AttemptedAt = DateTime.UtcNow;
    }

    public static EmailTask Create(NewEmailTask resource, Guid batchId, string clientName, Guid clientId,
        int counter) =>
        new(id: Guid.NewGuid())
        {
            BatchId = batchId,
            Counter = counter,
            ClientName = clientName,
            ClientId = clientId,
            From = resource.From,
            FromName = resource.FromName,
            Recipients = resource.Recipients,
            CopyRecipients = resource.CopyRecipients,
            Subject = resource.Subject,
            Body = resource.Body,
            IsHtml = resource.IsHtml,
        };

    public enum EmailStatus
    {
        Queued,
        Failed,
        Skipped,
        Sent,
    }
}
