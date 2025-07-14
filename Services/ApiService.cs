using AutoReportGenerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AutoReportGenerator.Services;

public class ApiService : IApiService
{
    private readonly ILogger<ApiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ApiService(ILogger<ApiService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<Dictionary<string, string>> GetApiSettingsAsync(string formId)
    {
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["Username"] = _configuration["ApiCredentials:Username"] ?? string.Empty,
                ["Password"] = _configuration["ApiCredentials:Password"] ?? string.Empty
            };

            _logger.LogDebug("Retrieved API settings for form {FormId}", formId);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API settings for form {FormId}", formId);
            throw;
        }
    }

    public async Task<List<DynamicDataObject>> GetApiDataAsync(string formId, DateTime runDate)
    {
        try
        {
            var records = new List<DynamicDataObject>();
            var settings = await GetApiSettingsAsync(formId);

            if (string.IsNullOrEmpty(settings["Username"]) || string.IsNullOrEmpty(settings["Password"]))
            {
                _logger.LogWarning("API credentials not configured for form {FormId}", formId);
                return records;
            }

            var apiData = await FetchApiDataAsync(formId, runDate, settings);
            records.AddRange(ProcessApiData(apiData));

            _logger.LogInformation("Retrieved {Count} records from API for form {FormId}", 
                records.Count, formId);

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from API for form {FormId}", formId);
            return new List<DynamicDataObject>();
        }
    }

    private async Task<string> FetchApiDataAsync(string formId, DateTime runDate, Dictionary<string, string> settings)
    {
        var apiEndpoint = $"https://api.example.com/forms/{formId}/data";
        var requestData = new
        {
            username = settings["Username"],
            password = settings["Password"],
            runDate = runDate.ToString("yyyy-MM-dd"),
            includeInstances = true
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestData);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(apiEndpoint, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private List<DynamicDataObject> ProcessApiData(string apiResponse)
    {
        var records = new List<DynamicDataObject>();

        try
        {
            var responseData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(apiResponse);
            
            if (responseData.TryGetProperty("memberInformation", out var memberData) && 
                memberData.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var member in memberData.EnumerateArray())
                {
                    var record = new DynamicDataObject();
                    ProcessMemberData(member, record);
                    records.Add(record);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing API response data");
        }

        return records;
    }

    private void ProcessMemberData(JsonElement member, DynamicDataObject record)
    {
        if (member.TryGetProperty("column", out var columns) && 
            columns.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var column in columns.EnumerateArray())
            {
                if (column.TryGetProperty("name", out var name) && 
                    column.TryGetProperty("value", out var value))
                {
                    record.AddProperty(name.GetString() ?? string.Empty, value.GetString());
                }
            }
        }

        if (member.TryGetProperty("instances", out var instances) && 
            instances.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var instance in instances.EnumerateArray())
            {
                if (instance.TryGetProperty("column", out var instanceColumns) && 
                    instanceColumns.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var column in instanceColumns.EnumerateArray())
                    {
                        if (column.TryGetProperty("name", out var name) && 
                            column.TryGetProperty("value", out var value))
                        {
                            record.AddProperty(name.GetString() ?? string.Empty, value.GetString());
                        }
                    }
                }
            }
        }
    }
} 