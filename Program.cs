using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AutoReportGenerator
{
    public class Program
    {
        public static string ConnectionString { get; private set; } = string.Empty;
        public static Dictionary<string, string> ApiCredentials { get; private set; } = new Dictionary<string, string>();

        public static async Task Main(string[] args)
        {
            try
            {
                await InitializeApplicationAsync();
                await ProcessReportsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
                await LogErrorAsync(ex);
            }
        }

        private static async Task InitializeApplicationAsync()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            ConnectionString = configuration["ConnectionStrings:Production"] ?? string.Empty;
            ApiCredentials = new Dictionary<string, string>
            {
                ["Username"] = configuration["ApiCredentials:Username"] ?? string.Empty,
                ["Password"] = configuration["ApiCredentials:Password"] ?? string.Empty
            };

            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new InvalidOperationException("Database connection string not configured");
            }
        }

        private static async Task ProcessReportsAsync()
        {
            var formsPath = Path.Combine(Directory.GetCurrentDirectory(), "forms.json");
            if (!File.Exists(formsPath))
            {
                Console.WriteLine("Forms configuration file not found");
                return;
            }

            var jsonContent = File.ReadAllText(formsPath);
            var forms = JsonConvert.DeserializeObject<List<ReportForm>>(jsonContent) ?? new List<ReportForm>();

            foreach (var form in forms)
            {
                if (ShouldProcessForm(form))
                {
                    await ProcessFormAsync(form);
                }
            }
        }

        private static bool ShouldProcessForm(ReportForm form)
        {
            var currentDate = DateTime.Today;
            
            if (form.Frequency == "D")
                return true;
            else if (form.Frequency == "W")
                return currentDate.DayOfWeek == DayOfWeek.Sunday;
            else if (form.Frequency == "M")
                return currentDate.Day == DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
            else
                return false;
        }

        private static async Task ProcessFormAsync(ReportForm form)
        {
            try
            {
                Console.WriteLine($"Processing form: {form.Name}");
                
                var reportService = new ReportService();
                var emailService = new EmailService();
                
                var reportData = await reportService.GenerateReportAsync(form.Id, DateTime.Today);
                
                if (reportData.Length > 0)
                {
                    var success = await emailService.SendReportEmailAsync(form, reportData);
                    Console.WriteLine($"Form {form.Name} processed successfully: {success}");
                }
                else
                {
                    Console.WriteLine($"No data to report for form: {form.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing form {form.Name}: {ex.Message}");
                await LogErrorAsync(ex);
            }
        }

        private static async Task LogErrorAsync(Exception ex)
        {
            var errorMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {ex.Message}\n{ex.StackTrace}\n";
            File.AppendAllText("error.log", errorMessage);
        }
    }
} 