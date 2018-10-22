using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using SfdcSvc;

namespace SfdcDataSync.Core.Adapter
{
    public class SfdcAdapter : ISfdcAdapter
    {
        private readonly BasicHttpBinding _binding;
        private SoapClient _soapClient;
        private static SessionHeader _sessionHeader;

        public SfdcConnectionParameter ConnectionParameter { get; set; }

        public SfdcCommand Command { get; set; }

        public IEnumerable<FieldMapping> Map { get; set; }

        public IResultLogger ResultLogger { get; set; }

        public SfdcAdapter()
        {
            _binding = new BasicHttpBinding
            {
                Security = { Mode = BasicHttpSecurityMode.Transport },
                MaxBufferPoolSize = 20000000,
                MaxBufferSize = 20000000,
                MaxReceivedMessageSize = 20000000,
                ReaderQuotas =
                {
                    MaxDepth = 32,
                    MaxArrayLength = 20000000,
                    MaxStringContentLength = 20000000
                }
            };
        }

        public SfdcAdapter(SfdcConnectionParameter connectionParameter) : this()
        {
            ConnectionParameter = connectionParameter ?? throw new ArgumentNullException(nameof(connectionParameter));
        }

        private async Task LoginAsync()
        {
            if (ConnectionParameter == null)
                throw new NotSupportedException($"{nameof(SfdcConnectionParameter)} can't be null");

            if (string.IsNullOrWhiteSpace(ConnectionParameter.UserName))
                throw new NotSupportedException($"{nameof(SfdcConnectionParameter)}.UserName can't be null");

            if (string.IsNullOrWhiteSpace(ConnectionParameter.Password))
                throw new NotSupportedException($"{nameof(SfdcConnectionParameter)}.Password can't be null");

            if (string.IsNullOrWhiteSpace(ConnectionParameter.Token))
                throw new NotSupportedException($"{nameof(SfdcConnectionParameter)}.Token can't be null");

            if (string.IsNullOrWhiteSpace(ConnectionParameter.Endpoint))
                throw new NotSupportedException($"{nameof(SfdcConnectionParameter)}.Endpoint can't be null");

            // configure SoapClient for login
            EndpointAddress loginEndpoint = new EndpointAddress(ConnectionParameter.Endpoint);
            SoapClient loginClient = new SoapClient(_binding, loginEndpoint);

            // try login to Sfdc
            loginResponse loginResponse;
            try
            {
                Logger.LogInfo("Login to Sfdc");
                loginResponse = await loginClient.loginAsync(null, null, ConnectionParameter.UserName,
                    ConnectionParameter.ApiPassword);
            }
            catch (Exception e)
            {
                throw new Exception("Failed login to Sfdc", e);
            }

            // check is password expired
            if (loginResponse.result.passwordExpired)
            {
                throw new Exception("Sfdc password expired");
            }

            // save session id
            _sessionHeader = new SessionHeader { sessionId = loginResponse.result.sessionId };

            // set SoapClient for Api interaction
            EndpointAddress endpoint = new EndpointAddress(loginResponse.result.serverUrl);
            _soapClient = new SoapClient(_binding, endpoint);

            Logger.LogInfo("Success login to Sfdc");
        }

        public async Task<object[]> SyncAsync(string soqlQuery, SqlSyncWriter writer)
        {
            if (string.IsNullOrWhiteSpace(soqlQuery))
                throw new ArgumentNullException(nameof(soqlQuery));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));


            // if not login yet then login
            if (_soapClient == null)
                await LoginAsync();

            List<object> writeResult = new List<object>();

            try
            {
                // read data from sfdc
                Logger.LogInfo("Querying from Sfdc");
                Logger.LogDebug($"SOQL: {soqlQuery}");
                queryResponse queryResponse =
                    await _soapClient.queryAsync(_sessionHeader, null, null, null, null, soqlQuery);
                QueryResult queryResult = queryResponse.result;

                if (queryResult.records == null)
                {
                    Logger.LogInfo($"No result return from Sfdc");
                    return null;
                }

                Logger.LogInfo(
                    $"Querying from Sfdc returning {queryResult.records.Length} records from {queryResult.size} total records");

                // loop fetch data from sfdc when is not done
                bool done;
                do
                {
                    done = queryResult.done;
                    // ReSharper disable once InvertIf
                    if (queryResult.size > 0)
                    {
                        // write data
                        DataTableObject data = new DataTableObject(queryResult.records);
                        Logger.LogInfo($"Writing to destination {data.Length} records");
                        object[] ids = await writer(data);
                        Logger.LogInfo($"Writing to destination completed");
                        writeResult.AddRange(ids);

                        // query more record
                        if (!queryResult.done)
                        {
                            Logger.LogInfo("Querying more record from Sfdc");
                            queryMoreResponse queryMoreResponse =
                                await _soapClient.queryMoreAsync(_sessionHeader, null, null, queryResult.queryLocator);
                            queryResult = queryMoreResponse.result;
                            Logger.LogInfo($"Querying more record from Sfdc returning {queryResult.records.Length} records");
                        }
                    }
                } while (!done);
            }
            catch (Exception e)
            {
                throw new Exception("Error sync data.", e);
            }

            return writeResult.ToArray();
        }

        public async Task<UpsertResult[]> UpsertAsync(DataTableObject data)
        {
            if (data == null || !data.Any())
                return null;

            if (Command == null)
                throw new NotSupportedException($"{nameof(Command)} can't be null");

            if (string.IsNullOrWhiteSpace(Command.Object))
                throw new NotSupportedException($"{nameof(Command.Object)} can't be null");

            if (string.IsNullOrWhiteSpace(Command.UpsertKeyField))
                throw new NotSupportedException($"{nameof(Command.UpsertKeyField)} can't be null");

            // if not login yet then login
            if (_soapClient == null)
                await LoginAsync();

            // upsert 
            sObject[] sObjects = data.ToSObjectArray(Command.Object, Map);

            Logger.LogInfo($"Executing Upsert {sObjects.Length} records to Sfdc using {Command.UpsertKeyField} as external id");

            Stopwatch stopwatch = Stopwatch.StartNew();
            upsertResponse upsertResponse = await _soapClient.upsertAsync(_sessionHeader,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                Command.UpsertKeyField,
                sObjects);
            stopwatch.Stop();

            Logger.LogInfo($"Executing Upsert completed on {stopwatch.ElapsedMilliseconds} ms");

            // logging result
            Logger.LogInfo($"Logging result");
            if (ResultLogger != null)
            {
                string keyField = Command.UpsertKeyField;

                if (Map != null && Map.Any())
                {
                    FieldMapping mapKeyField = Map.SingleOrDefault(x => x.To.Equals(Command.UpsertKeyField));
                    if (mapKeyField == null)
                        throw new Exception("KeyField not found on Mapping");
                    keyField = mapKeyField.From;
                }

                await ResultLogger.WriteLog(upsertResponse.result, data, keyField);
            }


            return upsertResponse.result;
        }
    }
}
