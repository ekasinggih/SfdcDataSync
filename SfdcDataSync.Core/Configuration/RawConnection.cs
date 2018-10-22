using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core
{
    public class RawConnection
    {
        public string Name { get; set; }

        public ConnectionType Type { get; set; }

        public object Parameter { get; set; }
    }
}
