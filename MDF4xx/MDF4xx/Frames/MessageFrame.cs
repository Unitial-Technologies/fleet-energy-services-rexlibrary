using System.Runtime.InteropServices;

namespace MDF4xx.Frames
{
    internal class MessageFrame : BaseDataFrame
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal new class FrameData : BaseDataFrame.FrameData
        {
        }

        internal new FrameData data { get => (FrameData)base.data; set => base.data = value; }

        public MessageFrame() : base()
        {
            data = new FrameData();
        }
    }
}
