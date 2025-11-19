using EmailQueue.API.Models;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace EmailQueue.API.Controllers;

[ApiController]
[Route("/")]
[Authorize(AuthenticationSchemes = nameof(ApiKeyAuthenticationHandler))]
public class EmailTasksWriteController(IQueueService queueService) : ControllerBase
{
    [HttpPost]
    [Route("add")]
    public async Task<IResult> EnqueueEmailsAsync([FromBody] NewEmailTask[] emailTasks)
    {
        if (emailTasks.Length == 0) return TypedResults.Ok(EnqueueEmailsResult.Empty());

        var batchId = await queueService.EnqueueEmailsAsync(emailTasks, User.ApiClientName(), User.ApiClientId());

        return TypedResults.Ok(EnqueueEmailsResult.Success(emailTasks.Length, batchId));
    }

    [HttpPost]
    [Route("add-to-batch")]
    public async Task<IResult> EnqueueEmailsForBatchAsync([FromBody] EmailsForBatchRequest request)
    {
        if (request.Emails.Length == 0) return TypedResults.Ok(EnqueueEmailsResult.Empty(request.BatchId));

        await queueService.EnqueueEmailsForBatchAsync(request.BatchId, request.Emails, User.ApiClientName(),
            User.ApiClientId());

        return TypedResults.Ok(EnqueueEmailsResult.Success(request.Emails.Length, request.BatchId));
    }
}

[UsedImplicitly]
public record EnqueueEmailsResult(string Status, int Count = 0, string BatchId = "")
{
    private static readonly EnqueueEmailsResult EmptyResult = new("Empty");
    public static EnqueueEmailsResult Empty() => EmptyResult;
    public static EnqueueEmailsResult Empty(Guid batchId) => new("Empty", BatchId: batchId.ToString());
    public static EnqueueEmailsResult Success(int count, Guid batchId) => new("Success", count, batchId.ToString());
}

public record EmailsForBatchRequest([property: JsonRequired] Guid BatchId, NewEmailTask[] Emails);
