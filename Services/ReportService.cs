using AutoReportGenerator.Models;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;

namespace AutoReportGenerator.Services;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseService _databaseService;
    private readonly IApiService _apiService;

    public ReportService(
        ILogger<ReportService> logger,
        IConfiguration configuration,
        IDatabaseService databaseService,
        IApiService apiService)
    {
        _logger = logger;
        _configuration = configuration;
        _databaseService = databaseService;
        _apiService = apiService;
    }

    public async Task<byte[]> GenerateReportAsync(string formId, DateTime runDate)
    {
        try
        {
            _logger.LogInformation("Generating report for form {FormId} with run date {RunDate}", formId, runDate);

            var databaseRecords = await _databaseService.GetFormDataAsync(formId, runDate);
            var apiRecords = await _apiService.GetApiDataAsync(formId, runDate);

            var combinedData = MergeDataSources(databaseRecords, apiRecords);
            return await CreateCsvReportAsync(combinedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for form {FormId}", formId);
            throw;
        }
    }

    public string GenerateFileName(string baseName)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{baseName} - {timestamp}.csv";
    }

    public async Task<List<ReportForm>> LoadFormsAsync()
    {
        try
        {
            var formsPath = Path.Combine(Directory.GetCurrentDirectory(), "forms.json");
            if (!File.Exists(formsPath))
            {
                _logger.LogWarning("Forms configuration file not found at {Path}", formsPath);
                return new List<ReportForm>();
            }

            var jsonContent = await File.ReadAllTextAsync(formsPath);
            var forms = JsonConvert.DeserializeObject<List<ReportForm>>(jsonContent) ?? new List<ReportForm>();

            foreach (var form in forms)
            {
                form.FileName = GenerateFileName(form.Name);
            }

            _logger.LogInformation("Loaded {Count} forms from configuration", forms.Count);
            return forms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading forms configuration");
            throw;
        }
    }

    private List<DynamicDataObject> MergeDataSources(
        List<DynamicDataObject> databaseRecords, 
        List<DynamicDataObject> apiRecords)
    {
        var mergedRecords = new List<DynamicDataObject>(databaseRecords);

        foreach (var apiRecord in apiRecords)
        {
            var matchingRecord = mergedRecords.FirstOrDefault(r => 
                GetPropertyValue(r, "EMAIL_ADDRESS")?.ToString() == 
                GetPropertyValue(apiRecord, "EMAIL_ADDRESS")?.ToString());

            if (matchingRecord != null)
            {
                MergeRecordProperties(matchingRecord, apiRecord);
            }
            else
            {
                mergedRecords.Add(apiRecord);
            }
        }

        return mergedRecords;
    }

    private void MergeRecordProperties(DynamicDataObject target, DynamicDataObject source)
    {
        var sourceDict = (IDictionary<string, object>)source.Instance;
        var targetDict = (IDictionary<string, object>)target.Instance;

        foreach (var kvp in sourceDict)
        {
            if (!targetDict.ContainsKey(kvp.Key))
            {
                targetDict[kvp.Key] = kvp.Value;
            }
        }
    }

    private dynamic? GetPropertyValue(DynamicDataObject record, string propertyName)
    {
        return record.GetProperty(propertyName);
    }

    private async Task<byte[]> CreateCsvReportAsync(List<DynamicDataObject> records)
    {
        if (!records.Any())
        {
            _logger.LogWarning("No records to export");
            return Array.Empty<byte>();
        }

        using var stringWriter = new StringWriter();
        using var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture);

        var firstRecord = records.First();
        var properties = ((IDictionary<string, object>)firstRecord.Instance).Keys.ToList();

        foreach (var property in properties)
        {
            csvWriter.WriteField(property);
        }
        csvWriter.NextRecord();

        foreach (var record in records)
        {
            var recordDict = (IDictionary<string, object>)record.Instance;
            foreach (var property in properties)
            {
                var value = recordDict.ContainsKey(property) ? recordDict[property] : string.Empty;
                csvWriter.WriteField(value);
            }
            csvWriter.NextRecord();
        }

        csvWriter.Flush();
        return Encoding.UTF8.GetBytes(stringWriter.ToString());
    }
} 