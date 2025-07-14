using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoReportGenerator
{
    public class ReportService
    {
        public async Task<byte[]> GenerateReportAsync(string formId, DateTime runDate)
        {
            try
            {
                Console.WriteLine($"Generating report for form {formId} with run date {runDate:yyyy-MM-dd}");

                var databaseRecords = await GetDatabaseDataAsync(formId, runDate);
                var apiRecords = await GetApiDataAsync(formId, runDate);

                var combinedData = MergeDataSources(databaseRecords, apiRecords);
                return CreateCsvReport(combinedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating report for form {formId}: {ex.Message}");
                throw;
            }
        }

        public string GenerateFileName(string baseName)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return $"{baseName} - {timestamp}.csv";
        }

        private async Task<List<DynamicDataObject>> GetDatabaseDataAsync(string formId, DateTime runDate)
        {
            var databaseService = new DatabaseService();
            return await databaseService.GetFormDataAsync(formId, runDate);
        }

        private async Task<List<DynamicDataObject>> GetApiDataAsync(string formId, DateTime runDate)
        {
            var apiService = new ApiService();
            return await apiService.GetApiDataAsync(formId, runDate);
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

        private dynamic GetPropertyValue(DynamicDataObject record, string propertyName)
        {
            return record.GetProperty(propertyName);
        }

        private byte[] CreateCsvReport(List<DynamicDataObject> records)
        {
            if (!records.Any())
            {
                Console.WriteLine("No records to export");
                return new byte[0];
            }

            using (var stringWriter = new StringWriter())
            using (var csvWriter = new CsvWriter(stringWriter, CultureInfo.InvariantCulture))
            {
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
    }
} 