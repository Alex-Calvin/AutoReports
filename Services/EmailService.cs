using AutoReportGenerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace AutoReportGenerator.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SendReportEmailAsync(ReportForm form, byte[] attachmentData)
    {
        try
        {
            if (attachmentData == null || attachmentData.Length == 0)
            {
                _logger.LogWarning("No attachment data provided for form {FormId}", form.Id);
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Report System", form.From));
            
            AddRecipients(message.To, form.To);
            AddRecipients(message.Cc, form.Cc);
            AddRecipients(message.Bcc, form.Bcc);

            message.Subject = form.FileName;

            var body = new TextPart("plain")
            {
                Text = "Please find the attached report with daily submissions for the requested form."
            };

            var attachment = new MimePart("text/csv")
            {
                Content = new MimeContent(new MemoryStream(attachmentData)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = form.FileName
            };

            var multipart = new Multipart("mixed");
            multipart.Add(body);
            multipart.Add(attachment);

            message.Body = multipart;

            await SendEmailAsync(message);
            
            _logger.LogInformation("Successfully sent report email for form {FormId} to {Recipients}", 
                form.Id, form.To);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending report email for form {FormId}", form.Id);
            return false;
        }
    }

    public async Task<bool> SendNotificationAsync(string to, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("System", _configuration["EmailSettings:DefaultFromAddress"]));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            await SendEmailAsync(message);
            
            _logger.LogInformation("Successfully sent notification to {Recipient}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to {Recipient}", to);
            return false;
        }
    }

    private void AddRecipients(InternetAddressList addressList, string recipients)
    {
        if (string.IsNullOrWhiteSpace(recipients))
            return;

        var emailAddresses = recipients.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var email in emailAddresses)
        {
            var trimmedEmail = email.Trim();
            if (!string.IsNullOrEmpty(trimmedEmail))
            {
                addressList.Add(new MailboxAddress("", trimmedEmail));
            }
        }
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"];
        var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort", 587);
        var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl", true);

        using var client = new SmtpClient();
        
        var secureSocketOptions = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        
        await client.ConnectAsync(smtpServer, smtpPort, secureSocketOptions);
        await client.AuthenticateAsync(_configuration["ApiCredentials:Username"], _configuration["ApiCredentials:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
} 