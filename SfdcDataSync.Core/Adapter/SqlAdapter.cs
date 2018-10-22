using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SfdcSvc;

namespace SfdcDataSync.Core.Adapter
{
    public class SqlAdapter : ISqlAdapter
    {
        private IEnumerable<FieldMapping> _map;

        private int[] _mapIndex = null;
        private string[] _fieldName = null;

        public SqlConnectionParameter ConnectionParameter { get; set; }
        public string Command { get; set; }

        public IEnumerable<FieldMapping> Map
        {
            get => _map;
            set
            {
                _map = value;
                _mapIndex = null;
                _fieldName = null;
            }
        }

        public IResultLogger ResultLogger { get; set; }

        public async Task<UpsertResult[]> SyncAsync(string sqlQuery, SfdcSyncWriter writer)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentNullException(nameof(sqlQuery));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (ConnectionParameter == null)
                throw new NotSupportedException($"{nameof(ConnectionParameter)} can't be null");

            if (string.IsNullOrWhiteSpace(ConnectionParameter.ConnectionString))
                throw new NotSupportedException($"{nameof(ConnectionParameter.ConnectionString)} can't be null");

            List<UpsertResult> results = new List<UpsertResult>();
            UpsertResult[] upsertResults;

            using (SqlConnection connection = new SqlConnection(ConnectionParameter.ConnectionString))
            {
                Logger.LogInfo("Querying from sql");
                Logger.LogDebug($"SQL: {sqlQuery}");
                try
                {
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    await connection.OpenAsync();

                    int index = 0;
                    List<object[]> data = new List<object[]>();
                    Dictionary<string, int> fieldsNameIndex = new Dictionary<string, int>();
                    Dictionary<int, string> fieldsIndexName = new Dictionary<int, string>();
                    DataTableObject dto;

                    // execute sql query
                    SqlDataReader dataReader = await command.ExecuteReaderAsync();
                    while (dataReader.Read())
                    {
                        int fieldsCount = dataReader.FieldCount;

                        // populate fields name and index
                        if (index == 0)
                        {
                            for (int i = 0; i < fieldsCount; i++)
                            {
                                string fieldName = dataReader.GetName(i);
                                fieldsNameIndex.Add(fieldName, i);
                                fieldsIndexName.Add(i, fieldName);
                            }
                        }

                        // populate field value
                        object[] row = new object[fieldsCount];
                        for (int i = 0; i < fieldsCount; i++)
                        {
                            bool isDbNull = await dataReader.IsDBNullAsync(i);
                            if (isDbNull)
                                row[i] = null;
                            else
                                row[i] = dataReader.GetValue(i);
                        }
                        data.Add(row);
                        index++;

                        // execute sync
                        if (ConnectionParameter.BatchSize > 0 && data.Count == ConnectionParameter.BatchSize)
                        {
                            dto = new DataTableObject(data, fieldsNameIndex, fieldsIndexName);
                            upsertResults = await writer(dto);
                            results.AddRange(upsertResults);
                            data.Clear();
                        }
                    }

                    // execute sync
                    if (data.Any())
                    {
                        dto = new DataTableObject(data, fieldsNameIndex, fieldsIndexName);
                        upsertResults = await writer(dto);
                        if (upsertResults != null)
                            results.AddRange(upsertResults);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Error sync data.", e);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return results.ToArray();
        }

        public async Task<object[]> ExecuteAsync(DataTableObject data)
        {
            if (data == null || !data.Any())
                return null;

            if (string.IsNullOrWhiteSpace(Command))
                throw new NotSupportedException($"{nameof(Command)} can't be null");

            Task<object>[] tasks = new Task<object>[data.Length];

            Logger.LogInfo($"Executing Command {Command} to Sql");
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < data.Length; i++)
            {
                tasks[i] = ExecuteSql(data, i);
            }

            object[] response = await Task.WhenAll(tasks);

            stopwatch.Stop();
            Logger.LogInfo($"Executing Command to sql completed on {stopwatch.ElapsedMilliseconds} ms");
            return response;
        }

        public async Task<int> ExecuteCommandAsync()
        {
            int result;
            // get connection from config
            using (SqlConnection cnn = new SqlConnection(ConnectionParameter.ConnectionString))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand(Command, cnn); ;

                    await cnn.OpenAsync();
                    result = await cmd.ExecuteNonQueryAsync();
                }
                finally
                {
                    if (cnn.State == ConnectionState.Open)
                        cnn.Close();
                }
            }

            return result;
        }

        private async Task<object> ExecuteSql(DataTableObject data, int index)
        {
            object result;
            // get connection from config
            using (SqlConnection cnn = new SqlConnection(ConnectionParameter.ConnectionString))
            {
                try
                {
                    IEnumerable<SqlParameter> parameters = BuildParameter(data, index);
                    SqlCommand cmd = new SqlCommand(Command, cnn); ;
                    cmd.Parameters.AddRange(parameters.ToArray());

                    await cnn.OpenAsync();
                    result = await cmd.ExecuteScalarAsync();
                }
                finally
                {
                    if (cnn.State == ConnectionState.Open)
                        cnn.Close();
                }
            }

            return result;
        }

        private IEnumerable<SqlParameter> BuildParameter(DataTableObject data, int index)
        {
            List<SqlParameter> param = new List<SqlParameter>();

            // get field mapping index
            if (Map != null && Map.Any())
            {
                if (_mapIndex == null)
                {
                    _mapIndex = new int[Map.Count()];
                    _fieldName = new string[Map.Count()];
                    int i = 0;
                    foreach (FieldMapping mapping in Map)
                    {
                        _mapIndex[i] = data.GetFieldIndex(mapping.From);
                        _fieldName[i] = mapping.To;
                        i++;
                    }
                }

                for (int i = 0; i < _mapIndex.Length; i++)
                {
                    param.Add(new SqlParameter { ParameterName = _fieldName[i], Value = data[index][_mapIndex[i]] });
                }
            }
            else
            {
                if (_fieldName == null)
                {
                    _fieldName = new string[data[0].Length];
                    for (int i = 0; i < data[0].Length; i++)
                    {
                        _fieldName[i] = data.GetFieldName(i);
                    }
                }

                for (int i = 0; i < _fieldName.Length; i++)
                {
                    param.Add(new SqlParameter { ParameterName = _fieldName[i], Value = data[index][i] });
                }
            }

            return param;
        }
    }
}
