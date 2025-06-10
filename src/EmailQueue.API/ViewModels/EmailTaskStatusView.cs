using EmailQueue.API.Models;

namespace EmailQueue.API.ViewModels;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public record EmailTaskStatusView
{
    // Constructors
    public EmailTaskStatusView(EmailTask e)
    {
        Id = e.Id;
        Counter = e.Counter;
        ClientName = e.ClientName;
        Status = e.Status;
        CreatedAt = e.CreatedAt;
        AttemptedAt = e.AttemptedAt;
        From = e.From;
        Recipients = e.Recipients;
        CopyRecipients = e.CopyRecipients;
        Subject = e.Subject;
    }

    // Properties
    public Guid Id { get; }
    public int Counter { get; }
    public string? ClientName { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime? AttemptedAt { get; }
    public string From { get; }
    public List<string> Recipients { get; }
    public List<string>? CopyRecipients { get; }
    public string Subject { get; }
}
