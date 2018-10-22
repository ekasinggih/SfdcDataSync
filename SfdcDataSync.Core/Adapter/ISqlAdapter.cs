using System.Collections.Generic;
using System.Threading.Tasks;
using SfdcSvc;

namespace SfdcDataSync.Core.Adapter
{
    public interface ISqlAdapter
    {
        SqlConnectionParameter ConnectionParameter { get; set; }
        string Command { get; set; }
        IEnumerable<FieldMapping> Map { get; set; }
        IResultLogger ResultLogger { get; set; }

        Task<UpsertResult[]> SyncAsync(string sqlQuery, SfdcSyncWriter writer);
        Task<object[]> ExecuteAsync(DataTableObject data);
        Task<int> ExecuteCommandAsync();
    }
}
