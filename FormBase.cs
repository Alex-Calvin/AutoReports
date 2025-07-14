using CsvHelper;
using LSUF.AutoReports.ControlService;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LSUF.AutoReports
{
    internal class FormBase
    {
        private readonly List<dynamic> ExportList = new List<dynamic>();
        private readonly List<DObject> Transactions = new List<DObject>();
        public FormBase() => Rundate = DateTime.MinValue.AddYears(2000);

        public string BCC { get; set; }
        public string CC { get; set; }
        public byte[] File { get; set; }
        public string Filename { get; set; }
        public string From { get; set; }
        public string ID { get; set; }
        public DateTime Rundate { get; set; }
        public string To { get; set; }
        public string Frequency { get; set; }

        //TODO:Create different classes to handle designations
        //TODO:Create a method to handle guest
        public byte[] Build(string formID, Dictionary<string, string> iModulesCreds)
        {
            using (ControlQuerySoapClient controlservice = new ControlQuerySoapClient())
            {
                using (OracleConnection conn = new OracleConnection(Program.ConnectionString))
                {
                    OracleCommand cmd = conn.CreateCommand();

                    //TODO: Move to external file
                    cmd.CommandText = $@"select
                                      prim_id_number as ADVANCE_ID,
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
                                      case when order_total is null
                                        then 'N'
                                      else 'Y' end as commerce
                                    from
                                      lsu_stg_imodules
                                    where
                                      form_id = '{formID}'
                                      and download_date > to_date('{Rundate.ToString("MM/dd/yyyy")}', 'MM/DD/YYYY')
                                    order by
                                      id desc";

                    try
                    {
                        conn.Open();
                        using (OracleDataReader odr = cmd.ExecuteReader())
                        {
                            if (odr.HasRows)
                            {
                                while (odr.Read())
                                {
                                    DObject transaction = new DObject();

                                    for (int i = 0; i < odr.FieldCount; i++)
                                    {
                                        transaction.AddProperty(odr.GetName(i), odr[i]);
                                    }

                                    Transactions.Add(transaction);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogError.AddLsuException(e);
                    }
                }

                var settings = GetControlSettings(iModulesCreds, formID, controlservice);

                //TODO: Split this up a little better to make it a bit more readable
                GetControlColumns(formID, iModulesCreds, controlservice);

            }
            return CreateCSV();
        }

        public virtual string GenerateFilename(string filename)
        {
            filename = filename + " - " + DateTime.Now.ToString("MM-dd-yyyy") + ".csv";
            return filename;
        }

        private void AddRows(IEnumerable<MemberInformation> memberrows, DObject transaction, bool iscommerce = true)
        {
            foreach (var row in memberrows)
            {
                foreach (var column in row.Column)
                {
                    if (transaction.GetProperty(column.Name.ToUpper()) == null)
                    {
                        transaction.AddProperty(column.Name, column.Value);
                    }
                }

                if (row.Instances != null && iscommerce)
                {
                    foreach (var instance in row.Instances)
                    {
                        foreach (var column in instance.Column)
                        {
                            if (transaction.GetProperty(column.Name.ToUpper()) == null)
                            {
                                transaction.AddProperty(column.Name, column.Value);
                            }
                        }
                    }
                }
            }
        }

        private string ColumnToString(AllColumnResults[] columnresults)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < columnresults.Count(); i++)
            {
                sb.Append(columnresults[i].Column.ToString());

                if (i < columnresults.Count() - 1)
                {
                    sb.Append(',');
                }
            }

            return sb.ToString();
        }

        private byte[] CreateCSV()
        {
            try
            {
                for (int i = 0; i < Transactions.Count(); i++)
                {
                    ExportList.Add(Transactions[i].Instance);
                }

                using (StringWriter sw = new StringWriter())
                {
                    using (CsvWriter csv = new CsvWriter(sw))
                    {
                        csv.WriteRecords(ExportList);

                        return Encoding.UTF8.GetBytes(sw.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                LogError.AddLsuException(e);
            }

            return null;
        }

        private void GetControlColumns(string formID, Dictionary<string, string> iModulesCreds, ControlQuerySoapClient controlservice)
        {
            try
            {
                int.TryParse(formID, out int form);
                var columnresults = controlservice.GetAllColumns(iModulesCreds["Username"], iModulesCreds["Password"], form).AllColumnResults;
                var rows = controlservice.GetMembersChangedSince(iModulesCreds["Username"], iModulesCreds["Password"], form, ColumnToString(columnresults), "", Rundate, true, true, true, true, true, "").MemberInformation;

                if (rows != null)
                {
                    var commercerows = from row in rows
                                       where row.Instances != null
                                       select row;

                    foreach (var transaction in Transactions)
                    {
                        if (!Convert.IsDBNull(transaction.Instance.COMMERCE_INSTANCE_UID))
                        {
                            var commercerow = from cr in commercerows
                                              from instance in cr.Instances
                                              where instance.Instance_Id.ToUpper() == transaction.Instance.COMMERCE_INSTANCE_UID.ToUpper()
                                              select cr;

                            AddRows(commercerow, transaction);
                        }
                        else
                        {
                            var row = from r in rows
                                      where r.Column.FirstOrDefault(column => column.Name.ToUpper() == "EMAIL_ADDRESS").Value.ToUpper() == transaction.Instance.EMAIL_ADDRESS.ToUpper()
                                      select r;

                            AddRows(row, transaction, false);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                LogError.AddLsuException(e);
            }
        }

        private Dictionary<string, string> GetControlSettings(Dictionary<string, string> iModulesCreds, string formID, ControlQuerySoapClient controlservice)
        {
            Dictionary<string, string> controlsettings = new Dictionary<string, string>();
            int.TryParse(formID, out int form);

            var controlSettingsResults = controlservice.GetControlSettings(iModulesCreds["Username"], iModulesCreds["Password"], form).ControlSettingsResults;

            foreach (var setting in controlSettingsResults)
            {
                controlsettings.Add(setting.Name, setting.Value);
            }

            return controlsettings;
        }
    }
}