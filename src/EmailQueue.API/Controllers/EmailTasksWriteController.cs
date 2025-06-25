using EmailQueue.API.Models;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace EmailQueue.API.Controllers;

[ApiController]
[Route("add")]
[Authorize(AuthenticationSchemes = nameof(SecuritySchemeType.ApiKey))]
public class EmailTasksWriteController(IQueueService queueService) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> EnqueueEmailsAsync([FromBody] NewEmailTask[] emailTasks)
    {
        if (emailTasks.Length == 0) return Results.Ok(EnqueueEmailsResult.Empty);

        var batchId = await queueService.EnqueueItems(emailTasks, User.ApiClientName(), User.ApiClientId());

        return batchId == null
            ? Results.Ok(EnqueueEmailsResult.Empty)
            : Results.Ok(new EnqueueEmailsResult("Success", emailTasks.Length, batchId.Value.ToString()));
    }
}

[UsedImplicitly]
public record EnqueueEmailsResult(string Status, int Count = 0, string BatchId = "")
{
    public static EnqueueEmailsResult Empty => new("Empty");
}
