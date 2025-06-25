using EmailQueue.API.Database;
using EmailQueue.API.Models;
using EmailQueue.API.Platform;
using GaEpd.EmailService;

namespace EmailQueue.API.Services;

public interface IEmailProcessorService
{
    Task ProcessEmailAsync(EmailTask email);
}

public class EmailProcessorService(
    IEmailService emailService,
    AppDbContext dbContext,
    ILogger<EmailProcessorService> logger)
    : IEmailProcessorService
{
    public async Task ProcessEmailAsync(EmailTask email)
    {
        logger.LogInformation("Processing email: {Counter} (at {TimeStamp})", email.Counter, DateTime.UtcNow);

        // Get a fresh instance of the task that is tracked by this context.
        var dbTask = await dbContext.EmailTasks.FindAsync(email.Id);
        if (dbTask == null)
        {
            logger.LogError("Email {Id} not found in database: {Counter}", email.Id, email.Counter);
            return;
        }

        if (AppSettings.EmailServiceSettings is { EnableEmail: false, EnableEmailAuditing: false })
        {
            dbTask.MarkAsSkipped();
            await dbContext.SaveChangesAsync();
            logger.LogWarning("Emailing is not enabled on the server");
            return;
        }

        if (email.Recipients.Count == 0 || email.Recipients.All(string.IsNullOrWhiteSpace))
        {
            dbTask.MarkAsFailed();
            await dbContext.SaveChangesAsync();
            logger.LogWarning("No recipient specified: {Counter}", email.Counter);
            return;
        }

        Message message;
        try
        {
            message = Message.Create(subject: email.Subject,
                recipients: email.Recipients,
                textBody: email.IsHtml ? null : email.Body,
                htmlBody: email.IsHtml ? email.Body : null,
                senderName: email.FromName,
                senderEmail: email.From,
                copyRecipients: email.CopyRecipients);
        }
        catch (Exception ex)
        {
            dbTask.MarkAsFailed();
            await dbContext.SaveChangesAsync();
            ex.Data.Add("Counter", email.Counter);
            throw;
        }

        try
        {
            await emailService.SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            dbTask.MarkAsFailed();
            await dbContext.SaveChangesAsync();
            ex.Data.Add("Counter", email.Counter);
            throw;
        }

        dbTask.MarkAsSent();
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Successfully sent email task: {Counter}", email.Counter);
    }
}
