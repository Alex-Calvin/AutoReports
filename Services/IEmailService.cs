using AutoReportGenerator.Models;

namespace AutoReportGenerator.Services;

public interface IEmailService
{
    Task<bool> SendReportEmailAsync(ReportForm form, byte[] attachmentData);
    Task<bool> SendNotificationAsync(string to, string subject, string body);
} 