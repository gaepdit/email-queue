namespace EmailQueue.API.ViewModels;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public record BatchStatusView
{
    public Guid BatchId { get; init; }
    public int Count { get; init; }
    public int Queued { get; init; }
    public int Sent { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public DateTime CreatedAt { get; init; }
}
