
using DbcParserLib;
using InfluxShared.FileObjects;
using System.Collections.Generic;

namespace DbcParserLib.Influx
{
    public static class DbcToInfluxObj
    {
        public static DBC FromDBC(Dbc dbc)
        {
            DBC influxDBC = new DBC();
            foreach (var msg in dbc.Messages)
            {
                DbcMessage msgI = new DbcMessage();
                msgI.CANID = msg.ID;
                msgI.DLC = msg.DLC;
                msgI.Comment = msg.Comment;
                msgI.Name = msg.Name;                
                msgI.MsgType = (DBCMessageType)(int)msg.Type;
                msgI.Transmitter = msg.Transmitter;
                
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
                    sigI.ValueType = sig.IsSigned == 1 ? DBCValueType.Signed : DBCValueType.Unsigned;
                    sigI.ItemType = 0;                    
                    //sigI.Mode = sig.Multiplexing

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
