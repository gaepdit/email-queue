using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Data;
using EmailQueue.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EmailQueue.API.Controllers;

[ApiController]
[Route("/")]
[Authorize(AuthenticationSchemes = nameof(SecuritySchemeType.ApiKey))]
public class EmailTasksReadController(EmailQueueDbContext dbContext) : ControllerBase
{
    [HttpGet("batches")]
    public async Task<ActionResult> GetAllBatchesAsync() =>
        Ok(await dbContext.EmailTasks
            .Where(t => t.ClientId == User.ApiClientId())
            .GroupBy(t => t.BatchId)
            .Select(g => new { BatchId = g.Key, Count = g.Count(), CreatedAt = g.Min(t => t.CreatedAt) })
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync());

    [HttpPost("batch")]
    [SuppressMessage("Performance",
        "CA1862:Use the \'StringComparison\' method overloads to perform case-insensitive string comparisons")]
    public async Task<ActionResult> GetBatchDetailsAsync([FromBody] BatchRequest request) =>
        Ok(await dbContext.EmailTasks
            .Where(t => t.BatchId == request.BatchId && t.ClientId == User.ApiClientId())
            .OrderBy(t => t.CreatedAt)
            .Select(t => new EmailTaskStatusView(t))
            .ToListAsync());

    public record BatchRequest([property: JsonRequired] Guid BatchId);
}
