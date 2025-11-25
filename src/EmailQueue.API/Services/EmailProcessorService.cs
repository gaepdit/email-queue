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
        logger.ZLogInformation($"Processing email: {email.Counter} (at {DateTime.UtcNow:@TimeStamp})");

        // Get a fresh instance of the task that is tracked by this context.
        var dbTask = await dbContext.EmailTasks.FindAsync(email.Id);
        if (dbTask == null)
        {
            logger.ZLogError($"Email {email.Id} not found in database: {email.Counter}");
            return;
        }

        if (AppSettings.EmailServiceSettings is { EnableEmail: false, EnableEmailAuditing: false })
        {
            const string skippedMessage = "Emailing is not enabled on the server";
            dbTask.MarkAsSkipped(skippedMessage);
            await dbContext.SaveChangesAsync();
            logger.ZLogWarning($"{skippedMessage}");
            return;
        }

        if (email.Recipients.Count == 0 || email.Recipients.All(string.IsNullOrWhiteSpace))
        {
            dbTask.MarkAsFailed("No recipients specified");
            await dbContext.SaveChangesAsync();
            logger.ZLogWarning($"No recipient specified: {email.Counter}", email.Counter);
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
            dbTask.MarkAsFailed(ex.Message);
            await dbContext.SaveChangesAsync();
            ex.Data.Add("Counter", email.Counter);
            ex.Data.Add("Id", email.Id);
            throw;
        }

        try
        {
            await emailService.SendEmailAsync(message);
        }
        catch (Exception ex)
        {
            dbTask.MarkAsFailed(ex.Message);
            await dbContext.SaveChangesAsync();
            ex.Data.Add("Counter", email.Counter);
            ex.Data.Add("Id", email.Id);
            throw;
        }

        dbTask.MarkAsSent();
        await dbContext.SaveChangesAsync();
        logger.ZLogInformation($"Successfully sent email task: {email.Counter}");
    }
}
