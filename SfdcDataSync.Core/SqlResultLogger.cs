using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SfdcDataSync.Core.Adapter;
using SfdcSvc;

namespace SfdcDataSync.Core
{
    public class SqlResultLogger : IResultLogger
    {
        private SqlResultLoggerConfig _sqlResultLoggerConfig;
        private SqlConnectionParameter _sqlConnectionParameter;

        public IEnumerable<RawConnection> Connections { get; set; }

        public void SetConfig(object config)
        {
            try
            {
                _sqlResultLoggerConfig = JsonConvert.DeserializeObject<SqlResultLoggerConfig>(config.ToString());
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing result logger config as SqlResultLoggerConfig", e);
            }

            _sqlConnectionParameter = Helper.GetSqlConnection(Connections, _sqlResultLoggerConfig.Connection);
        }

        public async Task WriteLog(SaveResult[] result, DataTableObject data, string keyFieldName)
        {
            throw new NotImplementedException();
        }

        public async Task WriteLog(UpsertResult[] result, DataTableObject data, string keyFieldName)
        {
            if (result == null)
                return;

            Task[] tasks = new Task[result.Length];
            for (int i = 0; i < result.Length; i++)
            {
                object keyValue = data[i][data.GetFieldIndex(keyFieldName)];
                string message = SerializeSfdcError(result[i].errors);
                Task task = WriteLog(keyValue, result[i].success, message, result[i].id);
                tasks[i] = task;
            }

            await Task.WhenAll(tasks);
        }

        public async Task WriteLog(DeleteResult[] result, DataTableObject data, string keyFieldName)
        {
            throw new NotImplementedException();
        }

        public async Task WriteLog(object[] result, DataTableObject data, string keyFieldName)
        {
            throw new NotImplementedException();
        }

        private async Task WriteLog(object key, object status, object message, object remoteKey)
        {
            // get connection from config
            using (SqlConnection cnn = new SqlConnection(_sqlConnectionParameter.ConnectionString))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(_sqlResultLoggerConfig.Command, cnn);
                    cmd.Parameters.Add(new SqlParameter { ParameterName = "@key", Value = key });
                    cmd.Parameters.Add(new SqlParameter { ParameterName = "@status", Value = status });
                    cmd.Parameters.Add(new SqlParameter { ParameterName = "@message", Value = message });
                    cmd.Parameters.Add(new SqlParameter { ParameterName = "@remote_key", Value = remoteKey ?? string.Empty });

                    await cnn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Logger.LogFatalException($"Error executing ResultLogger with params [key={key},status={status},message={message},remote_key={remoteKey}]", ex);
                }
                finally
                {
                    if (cnn.State == ConnectionState.Open)
                        cnn.Close();
                }
            }
        }

        private string SerializeSfdcError(Error[] errors)
        {
            if (errors == null || !errors.Any())
                return string.Empty;

            StringBuilder strBuilder = new StringBuilder();
            foreach (Error error in errors)
            {
                if (error.fields != null)
                {
                    strBuilder.Append("Fields: ");
                    foreach (string errorField in error.fields)
                    {
                        strBuilder.AppendFormat("{0}, ", errorField);
                    }
                }

                strBuilder.AppendFormat("Message: {0} || ", error.message);
            }

            return strBuilder.ToString();
        }
    }
}
