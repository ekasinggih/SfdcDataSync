using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core.Adapter
{
    public class SqlResultLoggerConfig
    {
        public string Connection { get; set; }

        /// <summary>
        /// Provide Sql Command to executed for each log record
        /// Using Param :
        /// @key -> for primary key mapping from sync data
        /// @status (boolean) -> sync status for each record
        /// @message -> error message for each record
        /// @remote_key -> created id
        /// sample :
        /// INSERT INTO tbl_log (sync_type, primary_key, remote_key, status, message)
        /// VALUES ('Contact', @key, @remote_key, @status, @message)
        /// </summary>
        public string Command { get; set; }
    }
}
