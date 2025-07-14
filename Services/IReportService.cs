using AutoReportGenerator.Models;

namespace AutoReportGenerator.Services;

public interface IReportService
{
    Task<byte[]> GenerateReportAsync(string formId, DateTime runDate);
    string GenerateFileName(string baseName);
    Task<List<ReportForm>> LoadFormsAsync();
} 