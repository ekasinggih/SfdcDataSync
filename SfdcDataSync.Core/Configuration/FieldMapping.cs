using System;
using System.Collections.Generic;
using System.Text;

namespace SfdcDataSync.Core
{
    public class FieldMapping
    {
        public FieldMapping()
        {
            UpdateOnNull = true;
        }

        public string From { get; set; }

        public string To { get; set; }

        public bool UpdateOnNull { get; set; }
    }
}
