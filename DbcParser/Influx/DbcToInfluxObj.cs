using InfluxShared.FileObjects;
using SharedObjects;
using System.Collections.Generic;

namespace DbcParserLib.Influx
{
    public static class DbcToInfluxObj
    {
        public static DBC FromList(List<DbcMessage> messages)
        {
            DBC influxDBC = new DBC();
            foreach (var msg in messages)
            {
                if (msg.CANID == 0xC0000000)
                    continue;
                DbcMessage msgI = new DbcMessage();
                msgI.CANID = msg.CANID;
                msgI.DLC = msg.DLC;
                msgI.Comment = msg.Comment;
                msgI.Name = msg.Name;
                msgI.MsgType = msg.MsgType;
                msgI.Transmitter = msg.Transmitter;

                influxDBC.Messages.Add(msgI);
                foreach (var sig in msg.Items)
                {
                    DbcItem sigI = new DbcItem();
                    sigI.Name = sig.Name;
                    sigI.Comment = sig.Comment;
                    sigI.ByteOrder = sig.ByteOrder == 0 ? DBCByteOrder.Motorola : DBCByteOrder.Intel;
                    sigI.StartBit = sig.StartBit;
                    sigI.BitCount = sig.BitCount;
                    sigI.Units = sig.Units;
                    sigI.MinValue = sig.MinValue;
                    sigI.MaxValue = sig.MaxValue;
                    sigI.Conversion.Type = ConversionType.Formula;
                    sigI.Conversion.Formula.CoeffB = sig.Factor;
                    sigI.Conversion.Formula.CoeffC = sig.Offset;
                    sigI.Conversion.Formula.CoeffF = 1;
                    sigI.Type = sig.Type;
                    sigI.ValueType = sig.ValueType;
                    sigI.ItemType = 0;
                    sigI.Ident = sig.Ident;
                    sigI.Parent = msgI;
                    sigI.Mode = sig.Mode;// Multiplexing

                    msgI.Items.Add(sigI);
                }

            }
            return influxDBC;
        }

        public static DBC FromDBC(Dbc dbc)
        {
            DBC influxDBC = new DBC();
            foreach (var msg in dbc.Messages)
            {
                if (msg.ID == 0xC0000000)
                    continue;
                DbcMessage msgI = new DbcMessage();
                msgI.CANID = msg.ID;
                msgI.DLC = msg.DLC;
                msgI.Comment = msg.Comment;
                msgI.Name = msg.Name;
                msgI.MsgType = msg.Type;
                msgI.Transmitter = msg.Transmitter;

                if (dbc.FileType == DBCFileType.KanCan)
                {
                    msgI.CANID = msg.ID & CanIdentifier.KanCanIdMask;
                    msgI.MsgType = DBCMessageType.KanCan;
                }

                influxDBC.Messages.Add(msgI);
                foreach (var sig in msg.Signals)
                {
                    DbcItem sigI = new DbcItem();
                    sigI.Name = sig.Name;
                    sigI.Comment = sig.Comment;
                    sigI.ByteOrder = sig.ByteOrder == 0 ? DBCByteOrder.Motorola : DBCByteOrder.Intel;
                    sigI.StartBit = sig.StartBit;
                    sigI.BitCount = sig.Length;
                    sigI.Units = sig.Unit;
                    sigI.MinValue = sig.Minimum;
                    sigI.MaxValue = sig.Maximum;
                    sigI.Conversion.Type = ConversionType.Formula;
                    sigI.Conversion.Formula.CoeffB = sig.Factor;
                    sigI.Conversion.Formula.CoeffC = sig.Offset;
                    sigI.Conversion.Formula.CoeffF = 1;
                    sigI.Type = DBCSignalType.Standard;
                    if (sig.Multiplexing is not null && sig.Multiplexing.Length > 0)
                    {
                        var mpstr = sig.Multiplexing.ToLower();
                        if (mpstr[0] == 'm')
                        {
                            if (mpstr.Length > 1)
                            {
                                mpstr = mpstr.Substring(1);
                                if (int.TryParse(mpstr, out int mode))
                                {
                                    sigI.Mode = (uint)mode;
                                    sigI.Type = DBCSignalType.ModeDependent;
                                }
                            }
                            else
                                sigI.Type = DBCSignalType.Mode;
                        }
                    }
                    sigI.ValueType = sig.ValueType;
                    sigI.ItemType = 0;
                    sigI.Ident = msg.ID;
                    sigI.Parent = msgI;

                    msgI.Items.Add(sigI);
                }

            }
            return influxDBC;
        }

        // Each dbc index in the list is for the corresponding CAN channel
        // So list[0] is for channel 0, list[1] is for channel 1 etc.
        public static ExportDbcCollection LoadExportSignalsFromDBC(List<DBC?> dbcList)
        {
            ExportDbcCollection signalsCollection = new ExportDbcCollection();
            for (byte i = 0; i < dbcList.Count; i++)
            {
                DBC? dbc = dbcList[i];
                if (dbc != null)
                {
                    foreach (var msg in dbc.Messages)
                    {
                        var expmsg = signalsCollection.AddMessage(i, msg);
                        foreach (var sig in msg.Items)
                            expmsg.AddSignal(sig);
                    }
                }
            }
            return signalsCollection;
        }
    }
}
