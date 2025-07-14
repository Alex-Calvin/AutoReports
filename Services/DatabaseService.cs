using AutoReportGenerator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace AutoReportGenerator.Services;

public class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseService(ILogger<DatabaseService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<List<DynamicDataObject>> GetFormDataAsync(string formId, DateTime runDate)
    {
        var records = new List<DynamicDataObject>();
        var connectionString = GetConnectionString();

        using var connection = new OracleConnection(connectionString);
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT 
                primary_id_number AS ADVANCE_ID,
                last_name,
                first_name,
                middle_name,
                email_address,
                street1,
                street2,
                city,
                state_code,
                zipcode,
                phone_number,
                order_total,
                purchase_date,
                commerce_instance_uid,
                CASE WHEN order_total IS NULL THEN 'N' ELSE 'Y' END AS commerce
            FROM staging_form_data
            WHERE form_id = :formId
                AND download_date > :runDate
            ORDER BY id DESC";

        command.Parameters.Add(":formId", OracleDbType.Varchar2).Value = formId;
        command.Parameters.Add(":runDate", OracleDbType.Date).Value = runDate;

        try
        {
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var record = new DynamicDataObject();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    record.AddProperty(columnName, value);
                }
                records.Add(record);
            }

            _logger.LogInformation("Retrieved {Count} records from database for form {FormId}", 
                records.Count, formId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from database for form {FormId}", formId);
            throw;
        }

        return records;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var connectionString = GetConnectionString();
            using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync();
            
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    private string GetConnectionString()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var connectionKey = environment switch
        {
            "Development" => "ConnectionStrings:Development",
            "Testing" => "ConnectionStrings:Testing",
            _ => "ConnectionStrings:Production"
        };

        var connectionString = _configuration[connectionKey];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string not found for environment: {environment}");
        }

        return connectionString;
    }
} 