using AutoReportGenerator.Models;

namespace AutoReportGenerator.Services;

public interface IApiService
{
    Task<Dictionary<string, string>> GetApiSettingsAsync(string formId);
    Task<List<DynamicDataObject>> GetApiDataAsync(string formId, DateTime runDate);
} 