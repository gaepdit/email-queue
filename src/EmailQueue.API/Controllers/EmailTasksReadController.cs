using EmailQueue.API.Database;
using EmailQueue.API.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

namespace EmailQueue.API.Controllers;

[ApiController]
[Route("/")]
[Authorize(AuthenticationSchemes = nameof(SecuritySchemeType.ApiKey))]
public class EmailTasksReadController(AppDbContext db) : ControllerBase
{
    private Guid ClientId => User.ApiClientId();

    [HttpGet("all-batches")]
    public async Task<IResult> GetAllBatchesAsync() =>
        Results.Ok(await db.GetAllBatchesAsync(ClientId));

    [HttpPost("batch-details")]
    public async Task<IResult> GetBatchDetailsAsync([FromBody] BatchRequest request) =>
        Results.Ok(await db.GetBatchDetailsAsync(ClientId, request.BatchId));

    [HttpPost("batch-status")]
    public async Task<IResult> GetBatchStatusAsync([FromBody] BatchRequest request)
    {
        var status = await db.GetBatchStatusAsync(ClientId, request.BatchId);
        return status is null ? Results.NotFound("Batch ID not found.") : Results.Ok(status);
    }
}

public record BatchRequest([property: JsonRequired] Guid BatchId);
