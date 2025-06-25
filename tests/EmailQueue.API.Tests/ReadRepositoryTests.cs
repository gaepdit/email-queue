using EmailQueue.API.Database;
using EmailQueue.API.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailQueue.API.Tests;

[TestFixture]
public class ReadRepositoryTests
{
    private AppDbContext _dbContext;

    private static List<EmailTask> _emailTasks(Guid batchId, Guid clientId) => [CreateEmailTask(batchId, clientId)];

    private static EmailTask CreateEmailTask(Guid batchId, Guid clientId) => EmailTask.Create(
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
        batchId: batchId, clientName: "Test Client", clientId: clientId, counter: 1);

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "ReadRepositoryTest").Options;
        _dbContext = new AppDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetAllBatches_ShouldReturnAllBatches()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        await _dbContext.EmailTasks.AddRangeAsync(_emailTasks(Guid.NewGuid(), clientId));
        await _dbContext.EmailTasks.AddRangeAsync(_emailTasks(Guid.NewGuid(), clientId));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await ReadRepository.GetAllBatchesAsync(_dbContext, clientId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetBatchDetails_ShouldReturnDetails()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        await _dbContext.EmailTasks.AddRangeAsync(_emailTasks(batchId, clientId));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await ReadRepository.GetBatchDetailsAsync(_dbContext, clientId, batchId);

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
        await _dbContext.EmailTasks.AddRangeAsync(_emailTasks(batchId, clientId));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await ReadRepository.GetBatchStatusAsync(_dbContext, clientId, batchId);

        // Assert
        using var scope = new AssertionScope();

        result.Should().NotBeNull();
        result.BatchId.Should().Be(batchId);
        result.Count.Should().Be(1);
    }
}
