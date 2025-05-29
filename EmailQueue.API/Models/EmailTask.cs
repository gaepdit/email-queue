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
    public string Status { get; private set; } = "Queued";

    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? AttemptedAt { get; private set; }

    // Methods
    public void MarkAsSent()
    {
        Status = "Sent";
        AttemptedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = "Failed";
        AttemptedAt = DateTime.UtcNow;
    }

    public static EmailTask Create(NewEmailTask resource, Guid batchId, string clientName, Guid clientId, int counter) =>
        new(id: Guid.NewGuid())
        {
            BatchId = batchId,
            Counter = counter,
            ClientName = clientName,
            ClientId = clientId,
            From = resource.From,
            Recipients = resource.Recipients,
            CopyRecipients = resource.CopyRecipients,
            Subject = resource.Subject,
            Body = resource.Body,
            IsHtml = resource.IsHtml,
        };
}
