using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    internal class LIN_DataFrame : BaseDataFrame
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

        public LIN_DataFrame() : base()
        {
            data = new FrameData();
            data.Type = DetectType();
        }
    }
}
