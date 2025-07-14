using LSUF.AutoReports;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LSUF.CrystalEmailer
{
    internal class Emailer
    {
        internal static void SendEmail(List<Form> forms)
        {
            using (OracleConnection conn = new OracleConnection(Program.ConnectionString))
            {
                OracleCommand cmd = conn.CreateCommand();

                try
                {
                    conn.Open();

                    foreach (var form in forms)
                    {
                        var frequencyDate = GetDate(DateTime.Today, form.Frequency);


                        if (form.File != null && form.File.Length > 0 && frequencyDate.Date == DateTime.Today.Date)
                        {
                            string body = string.Empty;
                            body = "Please see the attached report of daily submissions for the requested form.";

                            var em = new EmailManager.EmailManager(form.From);

                            string subject = $@"{form.Filename}";
                            var attachments = new List<EmailManager.IAttachment>();
                            var attachment = new EmailManager.Attachment(form.File, form.Filename);

                            attachments.Add(attachment);

                            IEnumerable<string> to = new List<string>();
                            IEnumerable<string> from = new List<string>();
                            IEnumerable<string> CC = new List<string>();
                            IEnumerable<string> BCC = new List<string>();

                            if (!string.IsNullOrEmpty(form.To))
                            {
                                to = form.To.Split(';');
                            }

                            if (!string.IsNullOrEmpty(form.From))
                            {
                                from = form.From.Split(';');
                            }

                            if (!string.IsNullOrEmpty(form.CC))
                            {
                                CC = form.CC.Split(';');
                            }

                            if (!string.IsNullOrEmpty(form.BCC))
                            {
                                BCC = form.BCC.Split(';');
                            }

                            var sentEmail = em.SendEmailAsync(subject, body, attachments, to, CC, BCC).Result;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError.AddLsuException(e);
                }
            }
        }

        internal static DateTime GetDate(DateTime today, string frequency)
        {
            var culture = Thread.CurrentThread.CurrentCulture;

            switch (frequency)
            {
                case "M":
                    DateTime firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    DateTime endOfMonth = firstOfMonth.AddMonths(1).AddTicks(-1);
                    return endOfMonth.Date;

                case "W":
                    DayOfWeek firstOfWeek = culture.DateTimeFormat.FirstDayOfWeek;
                    int offset = today.DayOfWeek - firstOfWeek;
                    DateTime firstDayOfWeek = today.AddDays(-offset).Date;
                    DateTime lastDayOfWeek = firstDayOfWeek.AddDays(7).AddTicks(-1);

                    return lastDayOfWeek.Date;

                case "D":
                    return today.Date;

                default:
                    firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endOfMonth = firstOfMonth.AddMonths(1).AddTicks(-1);
                    return endOfMonth.Date;
            }
        }
    }
}