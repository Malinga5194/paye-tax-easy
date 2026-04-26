using PayeTaxEasy.Core.Models;

namespace PayeTaxEasy.Infrastructure.Services;

public interface INotificationService
{
    Task SendSubmissionConfirmedAsync(string recipientId, SubmissionConfirmedPayload payload);
    Task SendSubmissionFailedAsync(string recipientId, SubmissionFailedPayload payload);
    Task SendDeadlineReminderAsync(string recipientId, string financialYear);
    Task SendMaintenanceWindowAsync(string recipientId, DateTime windowStart, DateTime windowEnd);
}
