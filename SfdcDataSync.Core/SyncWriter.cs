using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SfdcSvc;

namespace SfdcDataSync.Core
{
    public delegate Task<object[]> SqlSyncWriter(DataTableObject data);

    public delegate Task<UpsertResult[]> SfdcSyncWriter(DataTableObject data);
}
