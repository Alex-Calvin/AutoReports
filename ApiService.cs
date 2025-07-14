using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoReportGenerator
{
    public class ApiService
    {
        public async Task<List<DynamicDataObject>> GetApiDataAsync(string formId, DateTime runDate)
        {
            try
            {
                var records = new List<DynamicDataObject>();

                if (string.IsNullOrEmpty(Program.ApiCredentials["Username"]) || 
                    string.IsNullOrEmpty(Program.ApiCredentials["Password"]))
                {
                    Console.WriteLine($"API credentials not configured for form {formId}");
                    return records;
                }

                var apiData = await FetchApiDataAsync(formId, runDate);
                records.AddRange(ProcessApiData(apiData));

                Console.WriteLine($"Retrieved {records.Count} records from API for form {formId}");
                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving data from API for form {formId}: {ex.Message}");
                return new List<DynamicDataObject>();
            }
        }

        private async Task<string> FetchApiDataAsync(string formId, DateTime runDate)
        {
            await Task.Delay(100);
            
            return @"{
                ""memberInformation"": [
                    {
                        ""column"": [
                            {""name"": ""EMAIL_ADDRESS"", ""value"": ""test@example.com""},
                            {""name"": ""FIRST_NAME"", ""value"": ""John""},
                            {""name"": ""LAST_NAME"", ""value"": ""Doe""}
                        ],
                        ""instances"": [
                            {
                                ""column"": [
                                    {""name"": ""INSTANCE_ID"", ""value"": ""12345""},
                                    {""name"": ""STATUS"", ""value"": ""ACTIVE""}
                                ]
                            }
                        ]
                    }
                ]
            }";
        }

        private List<DynamicDataObject> ProcessApiData(string apiResponse)
        {
            var records = new List<DynamicDataObject>();

            try
            {
                var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponse);
                
                if (responseData.memberInformation != null)
                {
                    foreach (var member in responseData.memberInformation)
                    {
                        var record = new DynamicDataObject();
                        ProcessMemberData(member, record);
                        records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing API response data: {ex.Message}");
            }

            return records;
        }

        private void ProcessMemberData(dynamic member, DynamicDataObject record)
        {
            if (member.column != null)
            {
                foreach (var column in member.column)
                {
                    record.AddProperty(column.name.ToString(), column.value.ToString());
                }
            }

            if (member.instances != null)
            {
                foreach (var instance in member.instances)
                {
                    if (instance.column != null)
                    {
                        foreach (var column in instance.column)
                        {
                            record.AddProperty(column.name.ToString(), column.value.ToString());
                        }
                    }
                }
            }
        }
    }
} 