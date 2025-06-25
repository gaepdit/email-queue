using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Database;
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
        Results.Ok(await ReadRepository.GetAllBatchesAsync(db, ClientId));

    [HttpPost("batch-details")]
    public async Task<IResult> GetBatchDetailsAsync([FromBody] BatchRequest request) =>
        Results.Ok(await ReadRepository.GetBatchDetailsAsync(db, ClientId, request.BatchId));

    [HttpPost("batch-status")]
    public async Task<IResult> GetBatchStatusAsync([FromBody] BatchRequest request)
    {
        var status = await ReadRepository.GetBatchStatusAsync(db, ClientId, request.BatchId);
        return status is null ? Results.NotFound("Batch ID not found.") : Results.Ok(status);
    }
}

public record BatchRequest([property: JsonRequired] Guid BatchId);
