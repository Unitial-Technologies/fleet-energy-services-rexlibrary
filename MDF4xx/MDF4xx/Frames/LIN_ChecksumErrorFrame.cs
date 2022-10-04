using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    class LIN_ChecksumErrorFrame : BaseDataFrame
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal new class FrameData : BaseDataFrame.FrameData
        {
            public byte BusChannel;
            public byte LinID;
            public byte DLC;
            public byte Flags;
        }

        internal new FrameData data { get => (FrameData)base.data; set => base.data = value; }

        public LIN_ChecksumErrorFrame() : base()
        {
            data = new FrameData();
            data.Type = DetectType();
        }
    }
}
