using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core
{
    public class SyncTask
    {
        public string Name { get; set; }

        public SyncType Type { get; set; }

        public string SourceConnection { get; set; }

        public object SourceCommand { get; set; }

        public string TargetConnection { get; set; }

        public object TargetCommand { get; set; }

        public List<FieldMapping> Mapping { get; set; }

        public string ResultLogger { get; set; }
    }
}
