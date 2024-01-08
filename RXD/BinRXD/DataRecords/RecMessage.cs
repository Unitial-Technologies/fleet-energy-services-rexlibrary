using MDF4xx.Frames;
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

        internal new UInt32 RawTimestamp { get => data.Timestamp; set => data.Timestamp = value; }

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

        /*public override TraceCollection ToTraceRow(UInt32 TimestampPrecision)
        {
            var frames = base.ToTraceRow(TimestampPrecision);

            TraceRow trace = new TraceRow()
            {
                TraceType = LinkedBin.RecType,
                SourceName = LinkedBin.GetName,
                _Timestamp = (double)data.Timestamp * TimestampPrecision * 0.000001,
                _BusChannel = BusChannel,
                NotExportable = NotExportable,
                _DLC = header.DLC,
                _Data = new byte[header.DLC]
            };

            // Copy variable data
            Buffer.BlockCopy(VariableData, 0, trace._Data, 0, header.DLC);

            // Extract value
            var bindata = LinkedBin.GetDataDescriptor.CreateBinaryData();
            if (bindata.ExtractHex(VariableData, out BinaryData.HexStruct hex))
                trace._Value = bindata.CalcValue(ref hex);

            frames.Add(trace);

            return frames;
        }*/

    }
}
