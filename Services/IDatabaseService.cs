using AutoReportGenerator.Models;

namespace AutoReportGenerator.Services;

public interface IDatabaseService
{
    Task<List<DynamicDataObject>> GetFormDataAsync(string formId, DateTime runDate);
    Task<bool> TestConnectionAsync();
} 