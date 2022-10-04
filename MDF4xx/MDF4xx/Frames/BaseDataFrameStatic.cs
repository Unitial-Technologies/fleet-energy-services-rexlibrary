using System;
using System.Collections.Generic;

namespace MDF4xx.Frames
{
    public partial class BaseDataFrame
    {
        public static readonly List<byte> DlcFDList = new List<byte> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 12, 16, 20, 24, 32, 48, 64 };

        static readonly Dictionary<FrameType, Type> BlockClass = new Dictionary<FrameType, Type>()
        {
            { FrameType.Custom, typeof(BaseDataFrame) },
            { FrameType.CAN_DataFrame, typeof(CAN_DataFrame) },
            { FrameType.CAN_ErrorFrame, typeof(CAN_ErrorFrame) },
            { FrameType.LIN_DataFrame, typeof(LIN_DataFrame) },
            { FrameType.LIN_ChecksumErrorFrame, typeof(LIN_ChecksumErrorFrame) },
            { FrameType.LIN_TransmissionErrorFrame, typeof(LIN_TransmissionErrorFrame) },
        };

        public static readonly List<string> ErrorName = new()
        {
            "Other",
            "Bit Error",
            "Form Error",
            "Bit Stuffing Error",
            "CRC Error",
            "Acknowledgment Error",
        };
    }
}
