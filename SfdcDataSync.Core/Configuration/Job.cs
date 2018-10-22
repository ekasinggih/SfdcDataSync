using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core
{
    public class Job
    {
        public Job()
        {
            Connections = new List<RawConnection>();
            ResultLoggers = new List<ResultLoggerConfig>();
            Tasks = new List<SyncTask>();
        }

        public string Name { get; set; }

        public IList<RawConnection> Connections { get; set; }

        public IList<ResultLoggerConfig> ResultLoggers { get; set; }

        public IList<SyncTask> Tasks { get; set; }
    }
}
