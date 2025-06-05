using EmailQueue.API.Validation;
using System.Text.Json.Serialization;

namespace EmailQueue.API.Models;

public record NewEmailTask
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string From { get; init; }

    [JsonRequired]
    [MinLength(1)]
    [NoEmptyStrings]
    public List<string> Recipients { get; init; } = [];

    [NoEmptyStrings]
    public List<string>? CopyRecipients { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(200)]
    public required string Subject { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(20000)]
    public required string Body { get; init; }

    [JsonRequired]
    public bool IsHtml { get; init; }
}
