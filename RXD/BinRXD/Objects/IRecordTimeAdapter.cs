using System;

namespace RXD.Objects
{
    internal interface IRecordTimeAdapter
    {
        public UInt32 RawTimestamp { get; set; }

        public double FloatTimestamp { get; set; }
    }
}
