using EmailQueue.API.Controllers;
using EmailQueue.API.Models;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EmailQueue.API.Tests;

[TestFixture]
public class WriteControllerAddToBatchTests
{
    [Test]
    public async Task EmptyTaskList_ReturnsEmptyResult()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var request = new EmailsForBatchRequest(batchId, Emails: []);
        var controller = new EmailTasksWriteController(Substitute.For<IQueueService>());

        // Act
        var result = await controller.EnqueueEmailsForBatchAsync(request);

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<EnqueueEmailsResult>>();
        ((Ok<EnqueueEmailsResult>)result).Value.Should().Be(EnqueueEmailsResult.Empty(batchId));
    }

    [Test]
    public async Task SuccessfulEnqueuing_ReturnsSuccessResult()
    {
        // Arrange
        NewEmailTask[] emailTasks =
        [
            new()
            {
                From = "test@example.com",
                Subject = "subject",
                Body = "body",
            },
        ];

        var batchId = Guid.NewGuid();
        var request = new EmailsForBatchRequest(batchId, emailTasks);

        var queueServiceMock = Substitute.For<IQueueService>();

        var controller = new EmailTasksWriteController(queueServiceMock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity((List<Claim>)
                    [
                        new Claim(nameof(ApiClient.ClientName), "TestClientName"),
                        new Claim(nameof(ApiClient.ClientId), Guid.NewGuid().ToString()),
                    ], authenticationType: "TestAuthType")),
                },
            },
        };

        // Act
        var result = await controller.EnqueueEmailsForBatchAsync(request);

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<EnqueueEmailsResult>>();
        var enqueueEmailsResult = ((Ok<EnqueueEmailsResult>)result).Value!;
        enqueueEmailsResult.BatchId.Should().Be(batchId.ToString());
        enqueueEmailsResult.Status.Should().Be("Success");
        enqueueEmailsResult.Count.Should().Be(1);
    }
}
