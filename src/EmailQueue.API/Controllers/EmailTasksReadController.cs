using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Data;
using EmailQueue.API.Models;
using EmailQueue.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

namespace EmailQueue.API.Controllers;

[ApiController]
[Route("/")]
[Authorize(AuthenticationSchemes = nameof(SecuritySchemeType.ApiKey))]
public class EmailTasksReadController(EmailQueueDbContext dbContext) : ControllerBase
{
    [HttpGet("all-batches")]
    public async Task<ActionResult> GetAllBatchesAsync() =>
        Ok(await QueryByClientId()
            .GetBatchStatus()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync());

    [HttpPost("batch-details")]
    public async Task<ActionResult> GetBatchDetailsAsync([FromBody] BatchRequest request) =>
        Ok(await QueryByClientId()
            .Where(t => t.BatchId == request.BatchId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new EmailTaskStatusView(t))
            .ToListAsync());

    [HttpPost("batch-status")]
    public async Task<ActionResult> GetBatchStatusAsync([FromBody] BatchRequest request) =>
        Ok(await QueryByClientId()
            .Where(t => t.BatchId == request.BatchId)
            .GetBatchStatus()
            .SingleOrDefaultAsync());

    private IQueryable<EmailTask> QueryByClientId() =>
        dbContext.EmailTasks.Where(t => t.ClientId == User.ApiClientId());
}

public record BatchRequest([property: JsonRequired] Guid BatchId);

internal static class QueryableExtensions
{
    public static IQueryable<BatchStatusView> GetBatchStatus(this IQueryable<EmailTask> emailTasks) =>
        emailTasks.GroupBy(t => t.BatchId)
            .Select(g => new BatchStatusView
            {
                BatchId = g.Key,
                Count = g.Count(),
                Queued = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Queued)),
                Sent = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Sent)),
                Failed = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Failed)),
                Skipped = g.Count(t => t.Status == nameof(EmailTask.EmailStatus.Skipped)),
                CreatedAt = g.Min(t => t.CreatedAt),
            });
}
