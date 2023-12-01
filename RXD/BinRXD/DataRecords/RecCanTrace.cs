using MDF4xx.Frames;
using RXD.Objects;
using SharedObjects;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RXD.DataRecords
{
    [Flags]
    public enum MessageFlags : byte
    {
        /// <summary>
        /// Identifier Extension bit (Standard, Extended)						
        /// </summary>
        IDE = 1 << 0,
        /// <summary>
        /// Substitute Remote Request bit						
        /// </summary>
        SRR = 1 << 1,
        /// <summary>
        /// Extended Data Length bit (CANFD)						
        /// </summary>
        EDL = 1 << 2,
        /// <summary>
        /// Bit Rate Switch bit						
        /// </summary>
        BRS = 1 << 3,
        /// <summary>
        /// Direction (Rx, Tx)						
        /// </summary>
        DIR = 1 << 4,
    }

    internal class RecCanTrace : RecBase
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class DataRecord
        {
            public UInt32 Timestamp;
            public CanIdentifier CanID;
            public MessageFlags Flags;
        }

        internal new DataRecord data { get => (DataRecord)base.data; set => base.data = value; }

        public RecCanTrace()
        {
            data = new DataRecord();
        }

        public override List<BaseDataFrame> ToMdfFrame()
        {
            var frames = base.ToMdfFrame();
            if (NotExportable)
                return frames;

            CAN_DataFrame frame = new CAN_DataFrame();

            // Copy fixed length data
            frame.data.Timestamp = data.Timestamp;
            frame.data.BusChannel = (byte)(BusChannel + 1);
            frame.data.DLC = header.DLC;
            frame.data.DataBytes = header.DLC;
            frame.data.CanID = data.CanID;
            frame.data.Flags = (byte)data.Flags;

            // Copy variable data
            frame.VariableData = new byte[64];
            Buffer.BlockCopy(VariableData, 0, frame.VariableData, 0, header.DLC);

            frames.Add(frame);

            return frames;
        }

        public override MessageFrame ConvertToMdfMessageFrame(UInt16 GroupID, byte DLC)
        {
            MessageFrame frame = new MessageFrame();

            // Copy fixed length data
            frame.data.Timestamp = data.Timestamp;
            frame.data.Type = (FrameType)GroupID;

            // Copy variable data
            frame.VariableData = new byte[DLC];
            Buffer.BlockCopy(VariableData, 0, frame.VariableData, 0, Math.Min(DLC, header.DLC));

            return frame;
        }

        public override TraceCollection ToTraceRow(UInt32 TimestampPrecision)
        {
            var frames = base.ToTraceRow(TimestampPrecision);

            TraceRow trace = new TraceRow()
            {
                TraceType = LinkedBin.RecType,
                _Timestamp = (double)data.Timestamp * TimestampPrecision * 0.000001,
                _BusChannel = BusChannel,
                NotExportable = NotExportable,
                _CanID = data.CanID,
                flagIDE = data.Flags.HasFlag(MessageFlags.IDE),
                flagSRR = data.Flags.HasFlag(MessageFlags.SRR),
                flagEDL = data.Flags.HasFlag(MessageFlags.EDL),
                flagBRS = data.Flags.HasFlag(MessageFlags.BRS),
                flagDIR = data.Flags.HasFlag(MessageFlags.DIR),
                _DLC = header.DLC,
                _Data = new byte[header.DLC]
            };

            // Copy variable data
            Buffer.BlockCopy(VariableData, 0, trace._Data, 0, header.DLC);

            frames.Add(trace);

            return frames;
        }
    }
}
