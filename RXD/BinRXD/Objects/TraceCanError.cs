using InfluxShared.FileObjects;
using MDF4xx.Frames;

namespace RXD.Objects
{
    internal class TraceCanError : TraceCan, IRecordGridItem, ITraceConvertAdapter
    {
        public byte ErrorCode;
        public byte ErrorCount;

        public new string strCanID => "";
        public new string strData => $"{BaseDataFrame.ErrorName[ErrorCode]}, Code: {ErrorCode}, Count: {ErrorCount}";

        public override string asASCII
        {
            get
            {
                if (NotExportable)
                    return "";

                return string.Join(" ",
                    strTimestamp.PadLeft(20),
                    (BusChannel + 1).ToString().PadLeft(10),
                    "ErrorFrame",
                    "Flags = 0x2",
                    "CodeExt = 0x" + BLF.VectorErrorExt(ErrorCode).ToString("X4"),
                    "Code = 0x" + BLF.VectorError(ErrorCode).ToString("X2"),
                    "ID = 0",
                    "DLC = 0",
                    "Position = 0",
                    "Length = 0"
                );
            }
        }

        public override string asTRC
        {
            get
            {
                if (NotExportable)
                    return "";

                return string.Join(" ",
                    string.Format("{0:0.000}", FloatTimestamp * 1000).PadLeft(20),
                    (BusChannel + 1).ToString().PadLeft(10),
                    "ErrorFrame",
                    "Flags = 0x2",
                    "CodeExt = 0x" + BLF.VectorErrorExt(ErrorCode).ToString("X4"),
                    "Code = 0x" + BLF.VectorError(ErrorCode).ToString("X2"),
                    "ID = 0",
                    "DLC = 0",
                    "Position = 0",
                    "Length = 0"
                );
            }
        }
    }
}
