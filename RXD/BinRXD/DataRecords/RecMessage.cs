using InfluxShared.Objects;
using MDF4xx.Frames;
using RXD.Objects;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RXD.DataRecords
{
    internal class RecMessage : RecBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class DataRecord
        {
            public UInt32 Timestamp;
        }

        internal new DataRecord data { get => (DataRecord)base.data; set => base.data = value; }

        internal override UInt32 RawTimestamp { get => data.Timestamp; set => data.Timestamp = value; }

        public RecMessage()
        {
            data = new DataRecord();
        }

        public override List<BaseDataFrame> ToMdfFrame()
        {
            var frames = base.ToMdfFrame();

            MessageFrame frame = new MessageFrame();
            frame.data.Type = (FrameType)header.UID;

            // Copy fixed length data
            frame.data.Timestamp = data.Timestamp;

            // Copy variable data
            frame.VariableData = new byte[header.DLC];
            Buffer.BlockCopy(VariableData, 0, frame.VariableData, 0, header.DLC);

            frames.Add(frame);
            return frames;
        }

        public override TraceCollection ToTraceRow(UInt32 TimestampPrecision)
        {
            var frames = base.ToTraceRow(TimestampPrecision);

            TraceData trace = new TraceData()
            {
                SourceChannel = LinkedBin.GetName,
                RawTimestamp = data.Timestamp,
                FloatTimestamp = (double)data.Timestamp * TimestampPrecision * 0.000001,
                NotExportable = NotExportable,
            };

            // Extract value
            var bindata = LinkedBin.GetDataDescriptor.CreateBinaryData();
            if (bindata.ExtractHex(VariableData, out BinaryData.HexStruct hex))
                trace.Value = bindata.CalcValue(ref hex);

            frames.Add(trace);

            return frames;
        }

    }
}
