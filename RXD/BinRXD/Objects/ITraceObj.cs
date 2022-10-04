using RXD.DataRecords;
using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Objects
{
    interface ITraceObj
    {
        public RecordType TraceType { get; set; }

        public bool NotExportable { get; set; }

        public double Timestamp { get; set; }


    }
}
