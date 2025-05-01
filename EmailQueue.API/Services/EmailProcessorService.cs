﻿using EmailQueue.API.Data;
using EmailQueue.API.Models;
using EmailQueue.API.Settings;
using GaEpd.EmailService;
using GaEpd.EmailService.Utilities;

namespace EmailQueue.API.Services;

public interface IEmailProcessorService
{
    Task ProcessEmailAsync(EmailTask email);
}

public class EmailProcessorService(
    IEmailService emailService,
    EmailQueueDbContext dbContext,
    ILogger<EmailProcessorService> logger)
    : IEmailProcessorService
{
    public async Task ProcessEmailAsync(EmailTask email)
    {
        if (AppSettings.EmailServiceSettings is { EnableEmail: false, EnableEmailAuditing: false })
        {
            logger.LogWarning("Emailing is not enabled on the server");
            return;
        }

        logger.LogInformation("Processing email: {Counter}", email.Counter);

        // Get a fresh instance of the task that is tracked by this context.
        var dbTask = await dbContext.EmailTasks.FindAsync(email.Id);
        if (dbTask == null)
        {
            logger.LogError("Email {Id} not found in database: {Counter}", email.Id, email.Counter);
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
            message = Message.Create(email.Subject, email.Recipients,
                textBody: email.IsHtml ? null : email.Body,
                htmlBody: email.IsHtml ? email.Body : null,
                senderEmail: string.IsNullOrWhiteSpace(email.From) ? null : email.From,
                copyRecipients: email.CopyRecipients);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to create an email message from {Counter}", email.Counter);
            dbTask.MarkAsFailed();
            await dbContext.SaveChangesAsync();
            return;
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

public static class EmailServiceExtensions
{
    public static void AddEmailServices(this IServiceCollection services)
    {
        services.AddEmailService();
        services.AddScoped<IEmailProcessorService, EmailProcessorService>();
    }
}
