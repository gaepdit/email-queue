using EmailQueue.API.Database;
using EmailQueue.API.Models;
using EmailQueue.API.Platform;
using EmailQueue.API.Services;
using GaEpd.EmailService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZLogger.LogStates;

namespace EmailQueue.API.Tests;

[TestFixture]
public class EmailProcessorServiceTests
{
    private EmailProcessorService _sut;
    private IEmailService _emailService;
    private AppDbContext _dbContext;
    private ILogger<EmailProcessorService> _logger;
    private EmailTask _emailTask;

    [SetUp]
    public void Setup()
    {
        // Set up configuration and bind settings before creating any services
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "EmailServiceSettings:EnableEmail", "true" } })
            .Build().GetSection(nameof(AppSettings.EmailServiceSettings))
            .Bind(AppSettings.EmailServiceSettings);

        _emailService = Substitute.For<IEmailService>();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "EmailQueueTest").Options;
        _dbContext = new AppDbContext(options);

        _logger = Substitute.For<ILogger<EmailProcessorService>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _sut = new EmailProcessorService(_emailService, _dbContext, _logger);

        _emailTask = CreateEmailTask();

        // Set up the database with test data
        _dbContext.EmailTasks.Add(_emailTask);
        _dbContext.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private static EmailTask CreateEmailTask() => EmailTask.Create(
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
        batchId: Guid.NewGuid(), clientName: "Test Client", clientId: Guid.NewGuid(), counter: 1);

    [Test]
    public async Task ProcessEmailAsync_WhenEmailingDisabled_LogsWarningAndReturns()
    {
        // Arrange - set up configuration for disabled email
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
                { { "EmailServiceSettings:EnableEmail", "false" } })
            .Build().GetSection(nameof(AppSettings.EmailServiceSettings))
            .Bind(AppSettings.EmailServiceSettings);

        // Act
        await _sut.ProcessEmailAsync(_emailTask);

        // Assert
        using var scope = new AssertionScope();
        _logger.Received(1).Log(LogLevel.Information, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
        await _emailService.DidNotReceive().SendEmailAsync(Arg.Any<Message>());
    }

    [Test]
    public async Task ProcessEmailAsync_WhenEmailNotInDatabase_LogsErrorAndReturns()
    {
        // Arrange
        var nonExistentEmailTask = CreateEmailTask();

        // Act
        await _sut.ProcessEmailAsync(nonExistentEmailTask);

        // Assert
        _logger.Received(1).Log(LogLevel.Information, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
        _logger.Received(1).Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
        await _emailService.DidNotReceive().SendEmailAsync(Arg.Any<Message>());
    }

    [Test]
    public async Task ProcessEmailAsync_WhenNoRecipients_MarksAsFailed()
    {
        // Arrange
        var emailTask = CreateEmailTask();
        emailTask.Recipients.Clear();
        _dbContext.EmailTasks.Add(emailTask);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.ProcessEmailAsync(emailTask);

        // Assert
        emailTask.Status.Should().Be("Failed");
        _logger.Received(1).Log(LogLevel.Information, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
        await _emailService.DidNotReceive().SendEmailAsync(Arg.Any<Message>());
    }

    [Test]
    public async Task ProcessEmailAsync_WhenSendEmailFails_MarksAsFailedAndThrows()
    {
        // Arrange
        var expectedException = new Exception("Send failed");
        _emailService.When(x => x.SendEmailAsync(Arg.Any<Message>()))
            .Throw(expectedException);

        // Act
        var func = () => _sut.ProcessEmailAsync(_emailTask);

        // Assert
        await func.Should().ThrowAsync<Exception>();
        _emailTask.Status.Should().Be("Failed");
    }

    [Test]
    public async Task ProcessEmailAsync_WhenSuccessful_MarksAsSentAndSavesChanges()
    {
        // Act
        await _sut.ProcessEmailAsync(_emailTask);

        // Assert
        using var scope = new AssertionScope();
        _emailTask.Status.Should().Be("Sent");
        await _emailService.Received(1).SendEmailAsync(Arg.Is<Message>(m =>
            m.SenderEmail == _emailTask.From &&
            m.SenderName == _emailTask.FromName &&
            m.Recipients.Contains(_emailTask.Recipients[0]) &&
            m.CopyRecipients.Contains(_emailTask.CopyRecipients![0]) &&
            m.Subject == _emailTask.Subject &&
            m.TextBody == _emailTask.Body &&
            m.HtmlBody == null));
        _logger.Received(2).Log(LogLevel.Information, Arg.Any<EventId>(), Arg.Any<VersionedLogState>(), null,
            Arg.Any<Func<VersionedLogState, Exception?, string>>());
    }

    [Test]
    public async Task ProcessEmailAsyncWithNullFromName_WhenSuccessful_SendsMessageWithNullFromName()
    {
        // Arrange

        // Create email task with "From Name" property. 
        var emailTask = EmailTask.Create(CreateEmailTask() with { FromName = null },
            batchId: Guid.NewGuid(), clientName: "Test Client", clientId: Guid.NewGuid(), counter: 1);
        _dbContext.EmailTasks.Add(emailTask);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.ProcessEmailAsync(emailTask);

        // Assert
        await _emailService.Received(1).SendEmailAsync(Arg.Is<Message>(m => m.SenderName == null));
    }

    [Test]
    public async Task ProcessEmailAsyncWithEmptyFromName_WhenSuccessful_SendsMessageWithEmptyFromName()
    {
        // Arrange

        // Create an email task with empty "From Name" property. 
        var emailTask = EmailTask.Create(CreateEmailTask() with { FromName = string.Empty },
            batchId: Guid.NewGuid(), clientName: "Test Client", clientId: Guid.NewGuid(), counter: 1);
        _dbContext.EmailTasks.Add(emailTask);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.ProcessEmailAsync(emailTask);

        // Assert
        await _emailService.Received(1).SendEmailAsync(Arg.Is<Message>(m => m.SenderName == string.Empty));
    }
}
