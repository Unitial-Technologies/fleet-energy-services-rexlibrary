using RXD.Blocks;

namespace RXD.Objects
{
    internal interface ITraceObj
    {
        public RecordType TraceType { get; set; }

        public bool NotExportable { get; set; }

        public double Timestamp { get; set; }


    }
}
