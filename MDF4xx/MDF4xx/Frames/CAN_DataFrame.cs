using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    internal class CAN_DataFrame : BaseDataFrame
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal new class FrameData : BaseDataFrame.FrameData
        {
            public byte BusChannel;
            byte _DLC;
            public byte DLC { get => DlcFDList[_DLC]; set => _DLC = (byte)DlcFDList.IndexOf(value); }
            public byte DataBytes;
            public UInt32 CanID;
            public byte Flags;
        }

        internal new FrameData data { get => (FrameData)base.data; set => base.data = value; }

        public CAN_DataFrame() : base()
        {
            data = new FrameData();
            data.Type = DetectType();
        }
    }
}
