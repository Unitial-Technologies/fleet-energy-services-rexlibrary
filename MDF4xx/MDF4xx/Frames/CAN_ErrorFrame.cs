using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    public class CAN_ErrorFrame : BaseDataFrame
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal new class FrameData : BaseDataFrame.FrameData
        {
            public byte BusChannel;
            public byte Flags;
            public byte ErrorType;
            public byte ErrorCount;
        }

        internal new FrameData data { get => (FrameData)base.data; set => base.data = value; }

        public CAN_ErrorFrame() : base()
        {
            data = new FrameData();
            data.Type = DetectType();
        }
    }
}
