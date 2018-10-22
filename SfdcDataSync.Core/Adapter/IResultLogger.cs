using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SfdcSvc;

namespace SfdcDataSync.Core.Adapter
{
    public interface IResultLogger
    {
        IEnumerable<RawConnection> Connections { get; set; }

        void SetConfig(object config);

        Task WriteLog(SaveResult[] result, DataTableObject data, string keyFieldName);
        Task WriteLog(UpsertResult[] result, DataTableObject data, string keyFieldName);
        Task WriteLog(DeleteResult[] result, DataTableObject data, string keyFieldName);
        Task WriteLog(object[] result, DataTableObject data, string keyFieldName);
    }
}
