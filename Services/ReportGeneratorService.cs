using AutoReportGenerator.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoReportGenerator.Services;

public class ReportGeneratorService : BackgroundService
{
    private readonly ILogger<ReportGeneratorService> _logger;
    private readonly IReportService _reportService;
    private readonly IEmailService _emailService;
    private readonly PeriodicTimer _timer;

    public ReportGeneratorService(
        ILogger<ReportGeneratorService> logger,
        IReportService reportService,
        IEmailService emailService)
    {
        _logger = logger;
        _reportService = reportService;
        _emailService = emailService;
        _timer = new PeriodicTimer(TimeSpan.FromHours(1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Report Generator Service started");

        try
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                await ProcessReportsAsync();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Report Generator Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Report Generator Service");
        }
    }

    private async Task ProcessReportsAsync()
    {
        try
        {
            var forms = await _reportService.LoadFormsAsync();
            if (!forms.Any())
            {
                _logger.LogInformation("No forms configured for processing");
                return;
            }

            var currentDate = DateTime.Today;
            var processedCount = 0;

            foreach (var form in forms)
            {
                if (ShouldProcessForm(form, currentDate))
                {
                    await ProcessFormAsync(form, currentDate);
                    processedCount++;
                }
            }

            _logger.LogInformation("Processed {Count} forms", processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reports");
        }
    }

    private bool ShouldProcessForm(ReportForm form, DateTime currentDate)
    {
        return form.Frequency switch
        {
            "D" => true,
            "W" => currentDate.DayOfWeek == DayOfWeek.Sunday,
            "M" => currentDate.Day == DateTime.DaysInMonth(currentDate.Year, currentDate.Month),
            _ => false
        };
    }

    private async Task ProcessFormAsync(ReportForm form, DateTime runDate)
    {
        try
        {
            _logger.LogInformation("Processing form {FormId}: {FormName}", form.Id, form.Name);

            var reportData = await _reportService.GenerateReportAsync(form.Id, runDate);
            
            if (reportData.Length > 0)
            {
                var emailSent = await _emailService.SendReportEmailAsync(form, reportData);
                
                if (emailSent)
                {
                    _logger.LogInformation("Successfully processed and sent report for form {FormId}", form.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send email for form {FormId}", form.Id);
                }
            }
            else
            {
                _logger.LogInformation("No data to report for form {FormId}", form.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing form {FormId}", form.Id);
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
} 