using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core
{
    public enum ConnectionType
    {
        Sql = 0,
        Sfdc = 1
    }
    public enum SyncType
    {
        SqlToSfdc = 0,
        SfdcToSql = 1,
        SqlCommand = 2
    }

    public enum SfdcOperation
    {
        Insert = 0,
        Upsert = 1,
        Update = 2,
        Delete = 3,
        Select = 4
    }

    public enum ResultLoggerType
    {
        Sql
    }
}
