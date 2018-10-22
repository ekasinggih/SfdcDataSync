using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core.Adapter
{
    public class SqlConnectionParameter
    {
        public string ConnectionString { get; set; }

        public int BatchSize { get; set; }
    }
}
