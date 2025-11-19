using EmailQueue.API.Database;
using EmailQueue.API.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace EmailQueue.API.Controllers;

[ApiController]
[Route("/")]
[Authorize(AuthenticationSchemes = nameof(ApiKeyAuthenticationHandler))]
public class EmailTasksReadController(AppDbContext db) : ControllerBase
{
    private Guid ClientId => User.ApiClientId();

    [HttpGet("all-batches")]
    public async Task<IResult> GetAllBatchesAsync() =>
        TypedResults.Ok(await db.GetAllBatchesAsync(ClientId));

    [HttpPost("batch-details")]
    public async Task<IResult> GetBatchDetailsAsync([FromBody] BatchRequest request) =>
        TypedResults.Ok(await db.GetBatchDetailsAsync(ClientId, request.BatchId));

    [HttpPost("batch-status")]
    public async Task<IResult> GetBatchStatusAsync([FromBody] BatchRequest request)
    {
        var status = await db.GetBatchStatusAsync(ClientId, request.BatchId);
        return status is null ? TypedResults.NotFound("Batch ID not found.") : TypedResults.Ok(status);
    }

    [HttpPost("batch-failures")]
    public async Task<IResult> GetBatchFailedItemsAsync([FromBody] BatchRequest request) =>
        TypedResults.Ok(await db.GetBatchFailedItemsAsync(ClientId, request.BatchId));
}

public record BatchRequest([property: JsonRequired] Guid BatchId);
