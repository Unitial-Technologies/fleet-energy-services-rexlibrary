using RXD.DataRecords;
using System;

namespace RXD.Objects
{
    internal class TraceLin : TraceRow, IRecordGridItem, IItemGridDetails, ITraceConvertAdapter
    {
        public string strItemName => $"{strBusChannel} \\ {strCanID}";

        public string strTimestamp => string.Format("{0:0.000000}", FloatTimestamp);

        public byte BusChannel { get; set; }

        public string strBusChannel => "LIN " + BusChannel.ToString();

        public byte LinID { get; set; }

        public string strCanID => "0x" + LinID.ToString("X2");

        public bool flagDIR;
        public bool flagLPE;
        public bool flagLCSE;
        public bool flagLTE;
        public bool isError => flagLPE || flagLCSE || flagLTE;
        public string ErrorsString => ((flagLPE ? "Parity error, " : "") + (flagLCSE ? "Checksum error, " : "") + (flagLTE ? "Transmission error" : "")).Trim(' ', ',');

        public LinMessageFlags Flags
        {
            set
            {
                flagDIR = value.HasFlag(LinMessageFlags.DIR);
                flagLPE = value.HasFlag(LinMessageFlags.LPE);
                flagLCSE = value.HasFlag(LinMessageFlags.LCSE);
                flagLTE = value.HasFlag(LinMessageFlags.LTE);
            }
        }

        public string strFlags => flagDIR ? "     Tx" : "     Rx";

        public byte DLC { get; set; }

        public string strDLC => isError ? "" : DLC.ToString();

        public byte[] Data;

        public string strData => isError ? ErrorsString : BitConverter.ToString(Data).Replace("-", " ");

        public string asASCII
        {
            get
            {
                if (NotExportable)
                    return "";

                if (flagLPE)
                    return "";
                else if (flagLCSE)
                    return string.Join(" ",
                        strTimestamp.PadLeft(20),
                        ("L" + (BusChannel + 1).ToString()).PadLeft(10),
                        LinID.ToString("X2").PadLeft(10),
                        "CSErr".PadLeft(15),
                        (flagDIR ? " Tx" : " Rx").PadLeft(5),
                        (" " + DLC.ToString("X").PadLeft(6)).PadLeft(14),
                        BitConverter.ToString(Data).Replace("-", " "),
                        " checksum = 0"
                    );
                else if (flagLTE)
                    return string.Join(" ",
                        strTimestamp.PadLeft(20),
                        ("L" + (BusChannel + 1).ToString()).PadLeft(10),
                        LinID.ToString("X2").PadLeft(10),
                        "TransmErr".PadLeft(15)
                    );
                else if (!isError)
                    return string.Join(" ",
                        strTimestamp.PadLeft(20),
                        ("L" + (BusChannel + 1).ToString()).PadLeft(10),
                        LinID.ToString("X2").PadLeft(10),
                        (flagDIR ? " Tx" : " Rx").PadLeft(5),
                        (" " + DLC.ToString("X").PadLeft(6)).PadLeft(14),
                        BitConverter.ToString(Data).Replace("-", " "),
                        " checksum = 0"
                    );
                else
                    return "";
            }
        }

        public string asTRC => "";

        public string asCST => "";
    }
}
