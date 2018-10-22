using System.Collections.Generic;
using System.Threading.Tasks;
using SfdcSvc;

namespace SfdcDataSync.Core.Adapter
{
    public interface ISfdcAdapter
    {
        SfdcConnectionParameter ConnectionParameter { get; set; }
        SfdcCommand Command { get; set; }
        IEnumerable<FieldMapping> Map { get; set; }
        IResultLogger ResultLogger { get; set; }

        Task<object[]> SyncAsync(string soqlQuery, SqlSyncWriter writer);
        Task<UpsertResult[]> UpsertAsync(DataTableObject data);
    }
}
