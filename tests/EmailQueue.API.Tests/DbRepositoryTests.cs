using EmailQueue.API.Database;
using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Tests;

[TestFixture]
public class DbRepositoryTests
{
    private AppDbContext _db;

    private static EmailTask CreateEmailTask(Guid batchId, Guid clientId, int counter = 1) =>
        EmailTask.Create(
            new NewEmailTask
            {
                From = "test-from@example.com",
                FromName = "From Name",
                Recipients = ["test-to@example.com"],
                CopyRecipients = ["test-copy@example.net"],
                Subject = "Test Subject",
                Body = "Test Body",
                IsHtml = false,
            },
            batchId: batchId, clientName: "Test Client", clientId: clientId, counter: counter);

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

        await _db.SaveBatchAsync(
            CreateEmailTask(Guid.NewGuid(), clientId),
            CreateEmailTask(Guid.NewGuid(), clientId)
        );

        // Act
        var result = await _db.GetAllBatchesAsync(clientId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetBatchDetails_ShouldReturnDetails()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        await _db.SaveBatchAsync(CreateEmailTask(batchId, clientId));

        // Act
        var result = await _db.GetBatchDetailsAsync(clientId, batchId);

        // Assert
        using var scope = new AssertionScope();

        result.Should().ContainSingle();
        result[0].Counter.Should().Be(1);
    }

    [Test]
    public async Task GetBatchStatus_ShouldReturnStatus()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await _db.SaveBatchAsync(CreateEmailTask(batchId, clientId));

        // Act
        var result = await _db.GetBatchStatusAsync(clientId, batchId);

        // Assert
        using var scope = new AssertionScope();

        result.Should().NotBeNull();
        result.BatchId.Should().Be(batchId);
        result.Count.Should().Be(1);
    }

    [Test]
    public async Task GivenEmptyDatabase_GetMaxCounter_ShouldReturnZero()
    {
        // Act
        var result = await _db.GetMaxCounter();

        // Assert
        result.Should().Be(0);
    }

    [Test]
    public async Task GivenDatabaseWithData_GetMaxCounter_ShouldReturnMaxValue()
    {
        // Arrange
        const int counter = 99;
        var batchId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await _db.SaveBatchAsync(CreateEmailTask(batchId, clientId, counter));

        // Act
        var result = await _db.GetMaxCounter();

        // Assert
        result.Should().Be(counter);
    }

    [Test]
    public async Task GetQueuedItems_ShouldReturnQueuedItems()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        var queuedEmailTask = CreateEmailTask(Guid.NewGuid(), clientId);

        var sentEmailTask = CreateEmailTask(Guid.NewGuid(), clientId);
        sentEmailTask.MarkAsSent();

        var skippedEmailTask = CreateEmailTask(Guid.NewGuid(), clientId);
        skippedEmailTask.MarkAsSkipped();

        var failedEmailTask = CreateEmailTask(Guid.NewGuid(), clientId);
        failedEmailTask.MarkAsFailed(string.Empty);

        await _db.SaveBatchAsync(sentEmailTask, queuedEmailTask, skippedEmailTask, failedEmailTask);

        // Act
        var result = await _db.GetQueuedItems();

        // Assert
        using var scope = new AssertionScope();
        result.Should().ContainSingle();
        result.Should().OnlyContain(e => e.Status == nameof(EmailStatus.Queued));
    }
}
