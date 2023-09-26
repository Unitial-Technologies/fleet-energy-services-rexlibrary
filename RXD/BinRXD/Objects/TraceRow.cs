using InfluxShared.FileObjects;
using MDF4xx.Frames;
using RXD.Blocks;
using System;
using System.Collections.Generic;

namespace RXD.Objects
{
    public class TraceRow
    {
        public static readonly List<byte> DlcFDList = new List<byte> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 12, 16, 20, 24, 32, 48, 64 };

        public RecordType TraceType;
        public string SourceName;
        public UInt16 _DLC { get; set; }
        public string DLC
        {
            get
            {
                switch (TraceType)
                {
                    case RecordType.CanTrace:
                    case RecordType.CanError:
                        return _DLC.ToString();
                    case RecordType.LinTrace:
                        return LinError ? "" : _DLC.ToString();
                    case RecordType.MessageData:
                        return "";// _DLC.ToString();
                    case RecordType.Unknown:
                        return _DLC.ToString();
                    default:
                        return "";
                }
            }
        }

        public byte _BusChannel;
        public string BusChannel
        {
            get
            {
                switch (TraceType)
                {
                    case RecordType.CanTrace:
                    case RecordType.CanError:
                        return "CAN " + _BusChannel.ToString();
                    case RecordType.LinTrace:
                        return "LIN " + _BusChannel.ToString();
                    case RecordType.MessageData:
                        return SourceName;
                    default:
                        return "";
                }
            }
        }
        public bool NotExportable;

        public double _Timestamp;
        public string Timestamp
        {
            get
            {
                switch (TraceType)
                {
                    case RecordType.CanTrace:
                    case RecordType.CanError:
                    case RecordType.LinTrace:
                    case RecordType.MessageData:
                        return string.Format("{0:0.000000}", _Timestamp);
                    case RecordType.Unknown:
                        return "RAW";
                    default:
                        return "";
                }
            }
        }

        // TraceRecord fields
        public UInt32 _CanID;
        public bool flagIDE;
        public bool flagSRR;
        public bool flagEDL;
        public bool flagBRS;
        public bool flagDIR;

        // LinTraceError fields
        public bool flagLPE;
        public bool flagLCSE;
        public bool flagLTE;
        public bool LinError => flagLPE || flagLCSE || flagLTE;
        public string LinErrors => ((flagLPE ? "Parity error, " : "") + (flagLCSE ? "Checksum error, " : "") + (flagLTE ? "Transmission error" : "")).Trim(' ', ',');

        public byte[] _Data;
        public double _Value = double.NaN;

        // ErrorRecord fields
        public byte ErrorCode;
        public byte ErrorCount;

        // Grid output
        public string CanID
        {
            get
            {
                switch (TraceType)
                {
                    case RecordType.CanTrace:
                        return "0x" + (flagIDE ? _CanID.ToString("X8") : _CanID.ToString("X3"));
                    case RecordType.LinTrace:
                        return "0x" + _CanID.ToString("X2");
                    case RecordType.Unknown:
                        return "UID: " + _CanID.ToString();
                    default: 
                        return "";
                }
            }
        }

        public string Data
        {
            get
            {
                switch (TraceType)
                {
                    case RecordType.CanTrace:
                    case RecordType.LinTrace:
                        return LinError ? LinErrors : BitConverter.ToString(_Data).Replace("-", " ");
                    case RecordType.CanError: 
                        return $"{BaseDataFrame.ErrorName[ErrorCode]}, Code: {ErrorCode}, Count: {ErrorCount}";
                    case RecordType.PreBuffer: 
                        return "Trigger event";
                    case RecordType.MessageData:
                        return _Value.ToString();
                    case RecordType.Unknown:
                        return BitConverter.ToString(_Data).Replace("-", " ");
                    default: 
                        return "";
                }
            }
        }

        public string Flags
        {
            get
            {
                switch (TraceType)
                {
                    case RecordType.CanTrace:
                    case RecordType.CanError:
                        return (flagIDE ? "X" : " ") +
                        (flagSRR ? "R" : " ") +
                        (flagEDL ? flagBRS ? "FB" : "F " : "  ") +
                        (flagDIR ? " Tx" : " Rx");
                    case RecordType.LinTrace:
                        return flagDIR ? "     Tx" : "     Rx";
                    default:
                        return "";
                }
            }
        }

        public string asASCII
        {
            get
            {
                if (NotExportable)
                    return "";

                switch (TraceType)
                {
                    case RecordType.CanTrace:
                        if (flagEDL)
                            // CanFD
                            //Dlc:= frmGlobal.IVDReader.UnpackFDSize(MsgSize);
                            //identStr:= FastIntToHex(Identifier, outHexDigits[BusID]);
                            //if (Identifier > $7FF) then
                            //  identStr:= identStr + 'x';
                            //tmp:= rcAlignStr(FormatFloat(dvTimeFormat[tempIVD.TimePrecision], tempIVD.DoPrecision(RealTime) / 1000), 11, false) + ' CANFD ' + FastIntToStr(outBusPortID[BusID]) + ' ' + identStr +
                            //  ' Rx 1 0 d ' + FastIntToStr(MsgSize) + ' ' + FastIntToStr(Dlc) + ' ' + frmGlobal.IVDReader.MessageToStringFull(Data, true) + sLineBreak;
                            return string.Join(" ",
                                Timestamp.PadLeft(20),
                                ("CANFD " + (_BusChannel + 1).ToString()).PadLeft(10),
                                (_CanID.ToString("X8") + (flagIDE ? "x" : " ")).PadLeft(10),
                                (flagDIR ? " Tx" : " Rx").PadLeft(5),
                                ("1 0 d " + (DlcFDList.IndexOf((byte)_DLC).ToString() + " " + _DLC.ToString("X").PadLeft(3)).PadLeft(6)).PadLeft(14),
                                Data
                            );
                        else
                            // CAN
                            //identStr:= FastIntToHex(Identifier, outHexDigits[BusID]);
                            //if (Identifier > $7FF) then
                            //  identStr:= identStr + 'x';
                            //tmp:= rcAlignStr(FormatFloat(dvTimeFormat[tempIVD.TimePrecision], tempIVD.DoPrecision(RealTime) / 1000), 11, false) + ' ' + FastIntToStr(outBusPortID[BusID]) + ' ' + identStr +
                            //  ' Rx d ' + FastIntToStr(MsgSize) + ' ' + frmGlobal.IVDReader.MessageToStringFull(Data, true) + sLineBreak;
                            return string.Join(" ",
                                Timestamp.PadLeft(20),
                                (_BusChannel + 1).ToString().PadLeft(10),
                                (_CanID.ToString("X8") + (flagIDE ? "x" : " ")).PadLeft(10),
                                (flagDIR ? " Tx" : " Rx").PadLeft(5),
                                ("d " + _DLC.ToString("X").PadLeft(6)).PadLeft(14),
                                Data
                            );
                    case RecordType.CanError:
                        return string.Join(" ",
                            Timestamp.PadLeft(20),
                            (_BusChannel + 1).ToString().PadLeft(10),
                            "ErrorFrame",
                            "Flags = 0x2",
                            "CodeExt = 0x" + BLF.VectorErrorExt(ErrorCode).ToString("X4"),
                            "Code = 0x" + BLF.VectorError(ErrorCode).ToString("X2"),
                            "ID = 0",
                            "DLC = 0",
                            "Position = 0",
                            "Length = 0"
                        );
                    case RecordType.LinTrace:
                        if (flagLPE)
                            return "";
                        else if (flagLCSE)
                            // <Time> <Channel> <ID> CSErr <Dir> <DLC> <D0>...<D7> (slave = <slave id>, state = <state>) checksum = <checksum> header time = <header time>, full time = <full time>
                            return string.Join(" ",
                                Timestamp.PadLeft(20),
                                ("L" + (_BusChannel + 1).ToString()).PadLeft(10),
                                _CanID.ToString("X2").PadLeft(10),
                                "CSErr".PadLeft(15),
                                (flagDIR ? " Tx" : " Rx").PadLeft(5),
                                (" " + _DLC.ToString("X").PadLeft(6)).PadLeft(14),
                                BitConverter.ToString(_Data).Replace("-", " "),
                                " checksum = 0"
                            );
                        else if (flagLTE)
                            // <Time> <Channel> <ID> TransmErr (slave = <slave id>, state = <state>) header time = <header time> full time = <full time>
                            return string.Join(" ",
                                Timestamp.PadLeft(20),
                                ("L" + (_BusChannel + 1).ToString()).PadLeft(10),
                                _CanID.ToString("X2").PadLeft(10),
                                "TransmErr".PadLeft(15)
                            );
                        else if (!LinError)
                            // 0.073973 Li 2d Tx 8 00 f0 f0 ff ff ff ff ff checksum = 70 header time = 40, full time = 130 SOF = 0.067195
                            // BR = 19230 break = 937125 114062 EOH = 0.069266 EOB = 0.069789 0.070312 0.070835 0.071358 0.071881 0.072404 0.072927 0.073450
                            // sim = 1 EOF = 0.073973 RBR = 19231 HBR = 19230.769231 HSO = 26000 RSO = 26000 CSM = enhanced
                            return string.Join(" ",
                                Timestamp.PadLeft(20),
                                ("L" + (_BusChannel + 1).ToString()).PadLeft(10),
                                _CanID.ToString("X2").PadLeft(10),
                                (flagDIR ? " Tx" : " Rx").PadLeft(5),
                                (" " + _DLC.ToString("X").PadLeft(6)).PadLeft(14),
                                Data,
                                " checksum = 0"
                            );
                        break;
                }
                // FlexRay
                // Header:   0.039255 Fr RMSG 0 0 1 (1 = FlexRay Channel A; 2 = FlexRay Channel B; 3 = Any)
                // Message:  SlotID Cycle Rx 0 Flags4.3.5 CCType CCData 0 x PayloadLen, BufferLen, Data, 0, 0, 0
                //tmp:=
                //  rcAlignStr(FormatFloat(dvTimeFormat[tempIVD.TimePrecision], tempIVD.DoPrecision(RealTime) / 1000), 11, false) + ' Fr RMSG 0 0 1 ' + FastIntToStr(Channel + 1) +
                //  ' ' + FastIntToHex(Identifier, outHexDigits[BusID]) + ' ' + FastIntToHex(Cycle, 2) + ' Rx  0 100002 0 0 0 x ' + FastIntToHex(MsgSize, 2) + ' ' + FastIntToHex(MsgSize, 2) + ' ' +
                //  frmGlobal.IVDReader.MessageToStringFull(Data, true) + ' 0 0 0' + sLineBreak;
                return "";
            }
    }

        public string asTRC
        {
            get
            {
                if (NotExportable)
                    return "";

                switch (TraceType)
                {
                    case RecordType.CanTrace:
                        return string.Join(" ",
                            Timestamp.PadLeft(20),
                            flagEDL ? " FD " : " DT ",
                            (_BusChannel + 1).ToString().PadLeft(5),
                            (_CanID.ToString(flagIDE ? "X8" : "X4")).PadLeft(10),
                            (flagDIR ? " Tx" : " Rx").PadLeft(5),
                            DLC.PadLeft(10) + "   ",
                            Data
                        );
                    case RecordType.CanError:
                        return string.Join(" ",
                            Timestamp.PadLeft(20),
                            (_BusChannel + 1).ToString().PadLeft(10),
                            "ErrorFrame",
                            "Flags = 0x2",
                            "CodeExt = 0x" + BLF.VectorErrorExt(ErrorCode).ToString("X4"),
                            "Code = 0x" + BLF.VectorError(ErrorCode).ToString("X2"),
                            "ID = 0",
                            "DLC = 0",
                            "Position = 0",
                            "Length = 0"
                        );
                    default: return "";
                        // FlexRay
                        // Header:   0.039255 Fr RMSG 0 0 1 (1 = FlexRay Channel A; 2 = FlexRay Channel B; 3 = Any)
                        // Message:  SlotID Cycle Rx 0 Flags4.3.5 CCType CCData 0 x PayloadLen, BufferLen, Data, 0, 0, 0
                        //tmp:=
                        //  rcAlignStr(FormatFloat(dvTimeFormat[tempIVD.TimePrecision], tempIVD.DoPrecision(RealTime) / 1000), 11, false) + ' Fr RMSG 0 0 1 ' + FastIntToStr(Channel + 1) +
                        //  ' ' + FastIntToHex(Identifier, outHexDigits[BusID]) + ' ' + FastIntToHex(Cycle, 2) + ' Rx  0 100002 0 0 0 x ' + FastIntToHex(MsgSize, 2) + ' ' + FastIntToHex(MsgSize, 2) + ' ' +
                        //  frmGlobal.IVDReader.MessageToStringFull(Data, true) + ' 0 0 0' + sLineBreak;
                }
            }
        }
    }
}
