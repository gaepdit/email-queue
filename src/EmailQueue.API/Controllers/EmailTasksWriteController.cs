using EmailQueue.API.AuthHandlers;
using EmailQueue.API.Models;
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
    public async Task<IActionResult> EnqueueEmailsAsync([FromBody] NewEmailTask[] emailTasks)
    {
        var emptySubmissionResult = new { status = "Empty", count = 0, batchId = string.Empty };
        if (emailTasks.Length == 0) return Ok(emptySubmissionResult);

        var batchId = await queueService.EnqueueItems(emailTasks, User.ApiClientName(), User.ApiClientId());

        return batchId == null
            ? Ok(emptySubmissionResult)
            : Ok(new { status = "Success", count = emailTasks.Length, batchId });
    }
}
