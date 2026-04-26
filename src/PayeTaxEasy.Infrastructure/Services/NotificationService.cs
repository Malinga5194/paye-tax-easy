using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayeTaxEasy.Core.Models;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Entities;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PayeTaxEasy.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly PayeTaxEasyDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        PayeTaxEasyDbContext db,
        IConfiguration config,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task SendSubmissionConfirmedAsync(string recipientId, SubmissionConfirmedPayload payload)
    {
        string subject = "PAYE Submission Confirmed";
        string body = $"Your PAYE submission has been accepted.\n" +
                      $"Reference: {payload.ReferenceNumber}\n" +
                      $"Total PAYE: Rs. {payload.TotalPAYEAmount:N2}\n" +
                      $"Submitted: {payload.SubmittedAt:f}";

        await SendEmailAsync(recipientId, subject, body, "PayrollSubmissionAccepted");
    }

    public async Task SendSubmissionFailedAsync(string recipientId, SubmissionFailedPayload payload)
    {
        string subject = "PAYE Submission Failed";
        string body = $"Your PAYE submission failed.\n" +
                      $"Error: {payload.ErrorMessage}\n" +
                      $"Details: {string.Join(", ", payload.ValidationErrors)}";

        await SendEmailAsync(recipientId, subject, body, "PayrollSubmissionFailed");
    }

    public async Task SendDeadlineReminderAsync(string recipientId, string financialYear)
    {
        string subject = "PAYE Filing Deadline Reminder";
        string body = $"Reminder: The PAYE filing deadline for financial year {financialYear} " +
                      $"is in 7 days (30th November). Please submit your returns promptly.";

        await SendEmailAsync(recipientId, subject, body, "FilingDeadlineReminder");
    }

    public async Task SendMaintenanceWindowAsync(string recipientId, DateTime windowStart, DateTime windowEnd)
    {
        string subject = "PAYE Tax Easy — Scheduled Maintenance";
        string body = $"Scheduled maintenance: {windowStart:f} to {windowEnd:f} (UTC). " +
                      $"The system will be unavailable during this window.";

        await SendEmailAsync(recipientId, subject, body, "MaintenanceWindow");
    }

    private async Task SendEmailAsync(string recipientEmail, string subject, string body, string notificationType)
    {
        string status = "Sent";
        try
        {
            var apiKey = _config["SendGrid:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("SG.dev"))
            {
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("noreply@payetaxeasy.lk", "PAYE Tax Easy");
                var to = new EmailAddress(recipientEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);
                var response = await client.SendEmailAsync(msg);
                if ((int)response.StatusCode >= 400) status = "Failed";
            }
            else
            {
                // Dev mode — log instead of sending
                _logger.LogInformation("[DEV EMAIL] To: {To} | Subject: {Subject} | Body: {Body}",
                    recipientEmail, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipientEmail);
            status = "Failed";
        }

        _db.NotificationLogs.Add(new NotificationLog
        {
            RecipientId = recipientEmail,
            NotificationType = notificationType,
            Channel = "Email",
            Status = status,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
