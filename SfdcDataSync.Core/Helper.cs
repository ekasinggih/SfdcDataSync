using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SfdcDataSync.Core.Adapter;

namespace SfdcDataSync.Core
{
    internal class Helper
    {
        internal static RawConnection GetConnection(IEnumerable<RawConnection> connections, string connectionName)
        {
            RawConnection connection = connections.SingleOrDefault(x => x.Name.Equals(connectionName));
            if (connection == null)
                throw new Exception($"Connection with name {connectionName} not found");

            return connection;
        }

        internal static ResultLoggerConfig GetResultLogger(IEnumerable<ResultLoggerConfig> configs, string name)
        {
            ResultLoggerConfig logger = configs.SingleOrDefault(x => x.Name.Equals(name));
            if (logger == null)
                throw new Exception($"ResultLogger with name {name} not found");

            return logger;
        }

        internal static SqlConnectionParameter GetSqlConnection(IEnumerable<RawConnection> connections, string connectionName)
        {
            RawConnection raw = GetConnection(connections, connectionName);

            if (raw.Type != ConnectionType.Sql)
                throw new Exception($"Type of connection {connectionName} is not Sql connection type");

            string stringParam = raw.Parameter.ToString();
            SqlConnectionParameter sqlParam;
            try
            {
                sqlParam = JsonConvert.DeserializeObject<SqlConnectionParameter>(stringParam);
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing connection {connectionName} as Sql Connection", e);
            }

            return sqlParam;
        }

        internal static SfdcConnectionParameter GetSfdcConnection(IEnumerable<RawConnection> connections, string connectionName)
        {
            RawConnection raw = GetConnection(connections, connectionName);

            if (raw.Type != ConnectionType.Sfdc)
                throw new Exception($"Type of connection {connectionName} is not Sfdc connection type");

            string stringParam = raw.Parameter.ToString();
            SfdcConnectionParameter sfdcParam;
            try
            {
                sfdcParam = JsonConvert.DeserializeObject<SfdcConnectionParameter>(stringParam);
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing connection {connectionName} as Sfdc Connection", e);
            }

            return sfdcParam;
        }

        internal static SfdcCommand GetSfdcCommand(string commandText)
        {
            SfdcCommand command;
            try
            {
                command = JsonConvert.DeserializeObject<SfdcCommand>(commandText);
            }
            catch (Exception e)
            {
                throw new Exception("Error to read Sfdc command", e);
            }

            return command;
        }
    }
}
