using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoReportGenerator
{
    public class DatabaseService
    {
        public async Task<List<DynamicDataObject>> GetFormDataAsync(string formId, DateTime runDate)
        {
            var records = new List<DynamicDataObject>();

            using (var connection = new OracleConnection(Program.ConnectionString))
            using (var command = connection.CreateCommand())
            {
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
                    using (var reader = await command.ExecuteReaderAsync())
                    {
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
                    }

                    Console.WriteLine($"Retrieved {records.Count} records from database for form {formId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving data from database for form {formId}: {ex.Message}");
                    throw;
                }
            }

            return records;
        }
    }
} 