using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SfdcDataSync.Core.Adapter;
using SfdcSvc;

namespace SfdcDataSync.Core
{
    public class JobExecutor
    {
        private readonly ISqlAdapter _sqlAdapter;
        private readonly ISfdcAdapter _sfdcAdapter;
        private readonly IResultLogger _resultLogger;

        private Job _job;

        public JobExecutor(ISqlAdapter sqlAdapter, ISfdcAdapter sfdcAdapter, IResultLogger resultLogger)
        {
            _sqlAdapter = sqlAdapter;
            _sfdcAdapter = sfdcAdapter;
            _resultLogger = resultLogger;
        }

        public async Task ExecuteAsync(string configFile)
        {
            Job job;

            Logger.LogInfo($"Reading job configuration {configFile}");

            if (!File.Exists(configFile))
                throw new FileNotFoundException("Job file not found");

            try
            {
                string fileContent = File.ReadAllText(configFile);
                job = JsonConvert.DeserializeObject<Job>(fileContent);
            }
            catch (Exception e)
            {
                throw new Exception("Error to read job configuration", e);
            }

            await ExecuteAsync(job);
        }

        public async Task ExecuteAsync(Job job)
        {
            _job = job ?? throw new ArgumentNullException(nameof(job));

            Stopwatch jobStopwatch = Stopwatch.StartNew();
            Logger.LogInfo($"Executing job {job.Name}");

            foreach (SyncTask task in job.Tasks)
            {
                Stopwatch taskStopwatch = Stopwatch.StartNew();

                Logger.LogInfo($"Executing task {task.Name}");
                string output = String.Empty;

                // configure logger
                if (!string.IsNullOrWhiteSpace(task.ResultLogger))
                {
                    ResultLoggerConfig rsConfig = Helper.GetResultLogger(job.ResultLoggers, task.ResultLogger);
                    _resultLogger.Connections = job.Connections;
                    _resultLogger.SetConfig(rsConfig.Config);
                    _sqlAdapter.ResultLogger = _resultLogger;
                    _sfdcAdapter.ResultLogger = _resultLogger;
                }

                if (task.Type == SyncType.SqlToSfdc)
                {
                    var results = await SynchToSfdc(task);
                    if (results != null)
                        output = JsonConvert.SerializeObject(results);
                }
                else if (task.Type == SyncType.SfdcToSql)
                {
                    var results = await SynchToSql(task);
                    if (results != null)
                        output = JsonConvert.SerializeObject(results);
                }
                else if (task.Type == SyncType.SqlCommand)
                {
                    var results = await SqlCommand(task);
                    output = JsonConvert.SerializeObject(results);
                }

                //write ouput log
                string directoryPath = Path.Combine(Logger.OutputPath, job.Name, task.Name);
                string filePath = Path.Combine(directoryPath, string.Concat(DateTime.Now.ToString("yyyyMMdd_hhmmss"), ".log"));
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                File.WriteAllText(filePath, output);
                Logger.LogInfo($"Log created at {filePath}");

                taskStopwatch.Stop();
                Logger.LogInfo($"Task {task.Name} completed at {taskStopwatch.ElapsedMilliseconds / 1000} second");
            }

            jobStopwatch.Stop();
            Logger.LogInfo($"All Tasks at job {job.Name} completed at {jobStopwatch.ElapsedMilliseconds / 1000} second");
        }

        private async Task<UpsertResult[]> SynchToSfdc(SyncTask task)
        {
            SqlConnectionParameter sqlConnection = Helper.GetSqlConnection(_job.Connections, task.SourceConnection);
            SfdcConnectionParameter sfdcConnection = Helper.GetSfdcConnection(_job.Connections, task.TargetConnection);
            SfdcCommand command = Helper.GetSfdcCommand(task.TargetCommand.ToString());

            _sfdcAdapter.ConnectionParameter = sfdcConnection;
            _sfdcAdapter.Command = command;
            _sfdcAdapter.Map = task.Mapping;

            _sqlAdapter.ConnectionParameter = sqlConnection;

            UpsertResult[] results = await _sqlAdapter.SyncAsync(task.SourceCommand.ToString(), _sfdcAdapter.UpsertAsync);
            return results;
        }

        private async Task<object[]> SynchToSql(SyncTask task)
        {
            SqlConnectionParameter sqlConnection = Helper.GetSqlConnection(_job.Connections, task.TargetConnection);
            SfdcConnectionParameter sfdcConnection = Helper.GetSfdcConnection(_job.Connections, task.SourceConnection);
            SfdcCommand command = Helper.GetSfdcCommand(task.SourceCommand.ToString());

            _sqlAdapter.ConnectionParameter = sqlConnection;
            _sqlAdapter.Command = task.TargetCommand.ToString();
            _sqlAdapter.Map = task.Mapping;

            _sfdcAdapter.ConnectionParameter = sfdcConnection;
            _sfdcAdapter.Command = command;

            object[] results = await _sfdcAdapter.SyncAsync(command.CommandText, _sqlAdapter.ExecuteAsync);
            return results;
        }

        private async Task<int> SqlCommand(SyncTask task)
        {
            SqlConnectionParameter sqlConnection = Helper.GetSqlConnection(_job.Connections, task.TargetConnection);

            _sqlAdapter.ConnectionParameter = sqlConnection;
            _sqlAdapter.Command = task.TargetCommand.ToString();
            _sqlAdapter.Map = task.Mapping;

            int results = await _sqlAdapter.ExecuteCommandAsync();
            return results;
        }
    }
}
