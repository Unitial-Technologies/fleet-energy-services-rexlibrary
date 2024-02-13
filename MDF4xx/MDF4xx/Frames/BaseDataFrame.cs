using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    [Flags]
    public enum FrameType : UInt16
    {
        // Custom data - bit 15. All non custom data GroupID are referenced up to 15 bits (0 .. 32767)
        Custom = 1 << 15,
        // Specific data type from external inputs or sensors
        Specific = 1 << 14,

        // Bus type flags - these flags should be used only in combination with Custom flag
        Can = 0x1 << 8,
        Lin = 0x2 << 8,
        RlexRay = 0x3 << 8,

        // Predefined custom frame group ids based on flags
        CAN_DataFrame = Custom | Can | 0x1,
        CAN_ErrorFrame = Custom | Can | 0x2,
        CAN_RemoteFrame = Custom | Can | 0x3,
        CAN_OverloadFrame = Custom | Can | 0x4,
        LIN_DataFrame = Custom | Lin | 0x1,
        LIN_WakeUpFrame = Custom | Lin | 0x2,
        LIN_ChecksumErrorFrame = Custom | Lin | 0x3,
        LIN_TransmissionErrorFrame = Custom | Lin | 0x4,
        LIN_SyncErrorFrame = Custom | Lin | 0x5,
        LIN_ReceiveErrorFrame = Custom | Lin | 0x6,
        LIN_SpikeFrame = Custom | Lin | 0x7,
        LIN_LongDominantSignalFrame = Custom | Lin | 0x8,
        FR_Frame = Custom | RlexRay | 0x1,
        FR_PDUFrame = Custom | RlexRay | 0x2,
        FR_HeaderFrame = Custom | RlexRay | 0x3,
        FR_NullFrame = Custom | RlexRay | 0x4,
        FR_ErrorFrame = Custom | RlexRay | 0x5,
        FR_SymbolFrame = Custom | RlexRay | 0x6,

        // Predefined Influx Specific frame ids 
        IS_ADC = Custom | 0x1,
        IS_GPS = Custom | 0x2,
        IS_Accelerometer = Custom | 0x3,
        IS_Gyroscope = Custom | 0x4,
        IS_Digital = Custom | 0x5,
        IS_Analogue = Custom | 0x6,
    }

    public partial class BaseDataFrame
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class FrameData
        {
            public FrameType Type;
            public UInt64 Timestamp;
        }

        /// <summary>
        /// Fixed length Data block
        /// </summary>
        internal FrameData data;

        /// <summary>
        /// Variable length Data block
        /// </summary>
        internal byte[] VariableData;
        internal virtual int VariableDataSize { get => (VariableData == null) ? 0 : VariableData.Length; }

        public BaseDataFrame()
        {
        }

        protected FrameType DetectType()
        {
            return BlockClass.FirstOrDefault(x => x.Value == GetType()).Key;
        }

        public virtual byte[] ToBytes()
        {
            byte[] buffer = new byte[Marshal.SizeOf(data) + VariableDataSize];
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr p = h.AddrOfPinnedObject();
            Marshal.StructureToPtr(data, p, false);
            if (VariableData != null)
            {
                p += Marshal.SizeOf(data);
                Marshal.Copy(VariableData, 0, p, VariableDataSize);
            }
            h.Free();
            return buffer;
        }
    }
}
