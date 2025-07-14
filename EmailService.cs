using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AutoReportGenerator
{
    public class EmailService
    {
        public async Task<bool> SendReportEmailAsync(ReportForm form, byte[] attachmentData)
        {
            try
            {
                if (attachmentData == null || attachmentData.Length == 0)
                {
                    Console.WriteLine($"No attachment data provided for form {form.Id}");
                    return false;
                }

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(form.From);
                    
                    AddRecipients(message.To, form.To);
                    AddRecipients(message.CC, form.Cc);
                    AddRecipients(message.Bcc, form.Bcc);

                    message.Subject = form.FileName;
                    message.Body = "Please find the attached report with daily submissions for the requested form.";

                    var attachment = new Attachment(new MemoryStream(attachmentData), form.FileName, "text/csv");
                    message.Attachments.Add(attachment);

                    using (var client = new SmtpClient())
                    {
                        await Task.Run(() => client.Send(message));
                    }
                }
                
                Console.WriteLine($"Successfully sent report email for form {form.Id} to {form.To}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending report email for form {form.Id}: {ex.Message}");
                return false;
            }
        }

        private void AddRecipients(MailAddressCollection addressCollection, string recipients)
        {
            if (string.IsNullOrWhiteSpace(recipients))
                return;

            var emailAddresses = recipients.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var email in emailAddresses)
            {
                var trimmedEmail = email.Trim();
                if (!string.IsNullOrEmpty(trimmedEmail))
                {
                    addressCollection.Add(new MailAddress(trimmedEmail));
                }
            }
        }
    }
} 