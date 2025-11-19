using EmailQueue.API.Controllers;
using EmailQueue.API.Database;
using EmailQueue.API.Models;
using EmailQueue.API.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmailQueue.API.Tests;

[TestFixture]
public class ReadControllerTests
{
    private AppDbContext _db;

    private const string ClientName = nameof(ClientName);

    private static EmailTask CreateEmailTask(Guid batchId, Guid clientId, int counter = 1)
    {
        return EmailTask.Create(new NewEmailTask
        {
            From = "test-from@example.com",
            FromName = "From Name",
            Recipients = ["test-to@example.com"],
            CopyRecipients = ["test-copy@example.net"],
            Subject = "Subject",
            Body = "Body",
            IsHtml = false,
        }, batchId: batchId, clientName: ClientName, clientId: clientId, counter: counter);
    }

    private EmailTasksReadController GetController(Guid clientId) => new(_db)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity((List<Claim>)
                [
                    new Claim(nameof(ApiClient.ClientName), ClientName),
                    new Claim(nameof(ApiClient.ClientId), clientId.ToString()),
                ], authenticationType: "TestAuthType")),
            },
        },
    };

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: nameof(DbRepositoryTests)).Options;
        _db = new AppDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Test]
    public async Task GetAllBatches_ShouldReturnAllBatches()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var batchId1 = Guid.NewGuid();
        var batchId2 = Guid.NewGuid();

        await _db.SaveBatchAsync(CreateEmailTask(batchId1, clientId));
        await _db.SaveBatchAsync(CreateEmailTask(batchId2, clientId));

        var controller = GetController(clientId);

        // Act
        var result = await controller.GetAllBatchesAsync();

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<List<BatchStatusView>>>();
        var apiResult = ((Ok<List<BatchStatusView>>)result).Value;
        apiResult.Should().HaveCount(2);
        apiResult[0].BatchId.Should().Be(batchId2);
        apiResult[1].BatchId.Should().Be(batchId1);
    }

    [Test]
    public async Task GetAllBatches_WithDifferentClientIds_ShouldReturnCorrectBatches()
    {
        // Arrange
        var clientId1 = Guid.NewGuid();
        var batchId1 = Guid.NewGuid();
        var clientId2 = Guid.NewGuid();
        var batchId2 = Guid.NewGuid();

        await _db.SaveBatchAsync(CreateEmailTask(batchId1, clientId1));
        await _db.SaveBatchAsync(CreateEmailTask(batchId2, clientId2));

        var controller = GetController(clientId1);

        // Act
        var result = await controller.GetAllBatchesAsync();

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<List<BatchStatusView>>>();
        var apiResult = ((Ok<List<BatchStatusView>>)result).Value;
        apiResult.Should().ContainSingle();
        apiResult[0].BatchId.Should().Be(batchId1);
    }

    [Test]
    public async Task GetBatchDetails_ShouldReturnBatchDetails()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var emailTask = CreateEmailTask(batchId, clientId);
        await _db.SaveBatchAsync(emailTask);

        var controller = GetController(clientId);
        var batchRequest = new BatchRequest(batchId);

        // Act
        var result = await controller.GetBatchDetailsAsync(batchRequest);

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<List<EmailTaskStatusView>>>();
        var apiResult = ((Ok<List<EmailTaskStatusView>>)result).Value;
        apiResult.Should().ContainSingle();
        apiResult[0].Id.Should().Be(emailTask.Id);
    }

    [Test]
    public async Task GetBatchStatus_ShouldReturnBatchStatus()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var emailTask = CreateEmailTask(batchId, clientId);
        await _db.SaveBatchAsync(emailTask);

        var controller = GetController(clientId);
        var batchRequest = new BatchRequest(batchId);

        // Act
        var result = await controller.GetBatchStatusAsync(batchRequest);

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<BatchStatusView>>();
        var apiResult = ((Ok<BatchStatusView>)result).Value!;
        apiResult.BatchId.Should().Be(batchId);
    }

    [Test]
    public async Task GetBatchStatus_ForNonexistentBatch_ShouldReturnNull()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var batchId1 = Guid.NewGuid();
        var batchId2 = Guid.NewGuid();

        var emailTask = CreateEmailTask(batchId1, clientId);
        await _db.SaveBatchAsync(emailTask);

        var controller = GetController(clientId);
        var batchRequest = new BatchRequest(batchId2);

        // Act
        var result = await controller.GetBatchStatusAsync(batchRequest);

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<NotFound<string>>();
        var apiResult = ((NotFound<string>)result).Value;
        apiResult.Should().Be("Batch ID not found.");
    }

    [Test]
    public async Task GetBatchFailedItems_ShouldReturnFailedItems()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        var queuedEmailTask = CreateEmailTask(batchId, clientId);

        var failedEmailTask = CreateEmailTask(batchId, clientId);
        failedEmailTask.MarkAsFailed(string.Empty);

        await _db.SaveBatchAsync(queuedEmailTask, failedEmailTask);

        var controller = GetController(clientId);
        var batchRequest = new BatchRequest(batchId);

        // Act
        var result = await controller.GetBatchFailedItemsAsync(batchRequest);

        // Assert
        using var scope = new AssertionScope();

        result.Should().BeOfType<Ok<List<EmailTaskStatusView>>>();
        var apiResult = ((Ok<List<EmailTaskStatusView>>)result).Value;
        apiResult.Should().ContainSingle();
        apiResult[0].Id.Should().Be(failedEmailTask.Id);
    }
}
