using LSUF.JIRA;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.IO;
using System.Text;

namespace LSUF.AutoReports
{
    internal class LogError
    {
        internal static void AddLsuException(Exception e)
        {
            using (OracleConnection conn = new OracleConnection(Program.ConnectionString))
            {
                OracleCommand cmd = conn.CreateCommand();

                cmd.CommandText = $@"select count(table_name) from user_tables where table_name='LSU_EXCEPTION'";

                try
                {
                    int results = 0;

                    conn.Open();
                    results = Convert.ToInt32(cmd.ExecuteScalar());

                    if (results > 0)
                    {
                        cmd.CommandText = $@"ADVANCE.LSU_ADD_LSU_EXCEPTION";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("i_stack_trace", OracleDbType.Varchar2).Value = e.StackTrace.ToString();
                        cmd.Parameters.Add("i_message", OracleDbType.Varchar2).Value = e.Message.ToString();
                        cmd.Parameters.Add("i_create_ticket", OracleDbType.Varchar2).Value = "Y";
                        cmd.Parameters.Add("i_source", OracleDbType.Varchar2).Value = System.AppDomain.CurrentDomain.FriendlyName;

                        var x = cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        throw new Exception("Cannot connect to ADVANCE.LSU_ADD_LSU_EXCEPTION");
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        JiraManager jira = new JiraManager();
                        string ticket = jira.CreateTicket(e);
                    }
                    catch (Exception err)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($@"DateStamp: {DateTime.Now.ToString()}");
                        sb.AppendLine($@"Exception: {err.Message}");
                        sb.AppendLine($@"StackTrace: {err.StackTrace}");

                        if (ex.InnerException != null)
                        {
                            sb.AppendLine($@"Inner Exception: {err.InnerException}");
                        }

                        sb.AppendLine();
                        File.AppendAllText("errorlog.txt", sb.ToString());
                    }
                }
            }
        }
    }
}