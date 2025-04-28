using EmailQueue.API.Data;
using EmailQueue.API.Models;
using EmailQueue.API.Services;
using EmailQueue.API.Settings;
using GaEpd.EmailService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmailQueue.API.Tests.Services;

public class EmailProcessorServiceTests
{
    private EmailProcessorService _sut;
    private IEmailService _emailService;
    private EmailQueueDbContext _dbContext;
    private ILogger<EmailProcessorService> _logger;
    private EmailTask _emailTask;

    [SetUp]
    public void Setup()
    {
        // Set up configuration and bind settings before creating any services
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "EmailServiceSettings:EnableEmail", "true" },
                { "EmailServiceSettings:DefaultSenderEmail", "default@example.com" },
                { "EmailServiceSettings:DefaultSenderName", "Default Sender" },
            }).Build().GetSection(nameof(AppSettings.EmailServiceSettings))
            .Bind(AppSettings.EmailServiceSettings);

        _emailService = Substitute.For<IEmailService>();

        var options = new DbContextOptionsBuilder<EmailQueueDbContext>()
            .UseInMemoryDatabase(databaseName: "EmailQueueTest")
            .Options;
        _dbContext = new EmailQueueDbContext(options);

        _logger = Substitute.For<ILogger<EmailProcessorService>>();
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
            Body = "Test Body",
            Recipients = ["test@example.com"],
            From = "test@example.com",
            Subject = "Test Subject",
            IsHtml = false,
        },
        batchId: "1234567890", apiKeyOwner: "Test Owner", counter: 1);

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
        _logger.Received().Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<object>(), null,
            Arg.Any<Func<object, Exception?, string>>());
        await _emailService.DidNotReceive().SendEmailAsync(Arg.Any<Message>());
    }

    [Test]
    public async Task ProcessEmailAsync_WhenEmailNotFound_LogsErrorAndReturns()
    {
        // Arrange
        var nonExistentEmailTask = CreateEmailTask();

        // Act
        await _sut.ProcessEmailAsync(nonExistentEmailTask);

        // Assert
        _logger.Received().Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<object>(), null,
            Arg.Any<Func<object, Exception?, string>>());
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
        _logger.Received().Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<object>(), null,
            Arg.Any<Func<object, Exception?, string>>());
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
        _emailTask.Status.Should().Be("Sent");
        await _emailService.Received(1).SendEmailAsync(Arg.Is<Message>(m =>
            m.Subject == _emailTask.Subject &&
            m.Recipients.Contains(_emailTask.Recipients[0]) &&
            m.SenderEmail == _emailTask.From &&
            m.TextBody == _emailTask.Body &&
            m.HtmlBody == null));
        _logger.Received().Log(LogLevel.Information, Arg.Any<EventId>(), Arg.Any<object>(), null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
