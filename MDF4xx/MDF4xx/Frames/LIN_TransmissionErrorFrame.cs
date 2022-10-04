using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    class LIN_TransmissionErrorFrame : BaseDataFrame
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal new class FrameData : BaseDataFrame.FrameData
        {
            public byte BusChannel;
            public byte LinID;
        }

        internal new FrameData data { get => (FrameData)base.data; set => base.data = value; }

        public LIN_TransmissionErrorFrame() : base()
        {
            data = new FrameData();
            data.Type = DetectType();
        }
    }
}
