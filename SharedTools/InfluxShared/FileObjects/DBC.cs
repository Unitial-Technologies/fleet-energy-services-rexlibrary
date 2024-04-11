using InfluxShared.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace InfluxShared.FileObjects
{
    public enum DBCByteOrder : byte { Intel, Motorola }
    public enum DBCMessageType : byte { Standard, Extended, CanFDStandard, CanFDExtended, J1939PG, Lin, KanCan, reserved }
    public enum DBCValueType : byte { Unsigned, Signed, IEEEFloat, IEEEDouble, ASCII, BYTES }
    public enum DBCSignalType : byte { Standard, Mode, ModeDependent }
    public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay, KanCan };


    public class DbcSelection
    {
        public bool Log { get; set; }
        public DbcItem Item { get; set; }
    }

    public class DbcItem : BasicItemInfo
    {        
        public ushort StartBit { get; set; }
        public ushort BitCount { get; set; }
        public DBCSignalType Type { get; set; }
        public UInt32 Mode { get; set; }   //If the signal is Mode Dependent
        public DBCByteOrder ByteOrder { get; set; }
        public DBCValueType ValueType { get; set; }        

        public bool Log { get; set; }
        public override string ToString() => Name;
        public double Factor => Conversion.Type.HasFlag(ConversionType.Formula) ? Conversion.Formula.CoeffB : 1;
        public double Offset => Conversion.Type.HasFlag(ConversionType.Formula) ? Conversion.Formula.CoeffC : 0;
        public TableNumericConversion Table => Conversion.Type.HasFlag(ConversionType.TableNumeric) ? Conversion.TableNumeric : null;
        internal DbcMessage Parent { get; set; }

        public static bool operator ==(DbcItem item1, DbcItem item2) =>
            item1.StartBit == item2.StartBit &&
            item1.BitCount == item2.BitCount &&
            item1.Type == item2.Type &&
            item1.Mode == item2.Mode &&
            item1.ByteOrder == item2.ByteOrder &&
            item1.ValueType == item2.ValueType;// &&
            //item1.BytePos == item2.BytePos;
        public static bool operator !=(DbcItem item1, DbcItem item2) => !(item1 == item2);
        public override bool Equals(object obj)
        {
            if (obj is DbcItem)
                return this == (DbcItem)obj;
            else
                return false;
        }

        public DbcItem Clone => (DbcItem)MemberwiseClone();

        public override ChannelDescriptor GetDescriptor => new ChannelDescriptor()
        {
            StartBit = StartBit,
            BitCount = BitCount,
            isIntel = ByteOrder == DBCByteOrder.Intel,
            HexType = BinaryData.BinaryTypes[(int)ValueType],
            conversionType = Conversion.Type,
            Factor = Factor,
            Offset = Offset,
            Table = Table,
            Name = Name,
            Units = Units
        };
    }

    public class DbcMessage
    {
        private uint _canId;
        private Regex BadCharacters = new Regex("[^a-zA-Z0-9_]");
        public string CleanName { get => BadCharacters.Replace(Name, ""); }

        public string Name { get ; set ; }
        public uint CANID { get => _canId; set => SetCanID(value); }

        private void SetCanID(uint value)
        {
            _canId = value;
            foreach (var item in Items)
            {
                item.Ident = _canId;
            }
        }
        
        public string HexIdent => "0x" + (isExtended ? CANID.ToString("X8") : CANID.ToString("X3"));
        public byte DLC { get; set; }
        public DBCMessageType MsgType { get; set; }
        public bool isExtended => MsgType == DBCMessageType.Extended || MsgType == DBCMessageType.CanFDExtended || MsgType == DBCMessageType.J1939PG;
        public string Transmitter { get; set; }
        public string Comment { get; set; }
        public List<DbcItem> Items { get; set; }
        public bool Log { get; set; }

        public uint FullID => isExtended ? (uint)(CANID | (1 << 31)) : CANID;

        public static bool operator ==(DbcMessage item1, DbcMessage item2) => !(item1 is null) && !(item2 is null) && item1.MsgType == item2.MsgType && item1.CANID == item2.CANID;
        public static bool operator !=(DbcMessage item1, DbcMessage item2) => !(item1 == item2);
        public override bool Equals(object obj)
        {
            if (obj is DbcMessage)
                return this == (DbcMessage)obj;
            else
                return false;
        }

        public DbcMessage()
        {
            Items = new List<DbcItem>();
        } 
    }

    public class DBC
    {
        public DBCFileType FileType { get; set; }
        public string FileName { get; set; }
        public string FileNameSerialized { get; set; }  //Imeto na DBC-to zapisano kato serialized file
        public string FileNameNoExt => Path.GetFileNameWithoutExtension(FileName);
        public string FileLocation => Path.GetDirectoryName(FileName);
        public List<DbcMessage> Messages { get; set; }

        public bool Equals(DBC dbc)
        {
            if (this != null && dbc != null)
            {
                if (this.FileNameSerialized == dbc.FileNameSerialized)
                    return true;
            }
            return false;
        } 

        private void WriteMessages(StreamWriter sw, List<DbcMessage> messages)
        {
            sw.WriteLine("");

            string line;
            foreach (DbcMessage msg in messages)
            {
                string transmitter = msg.Transmitter != null ? msg.Transmitter : "Vector__XXX";
                line = "BO_ " + msg.CANID.ToString() + " " + msg.CleanName + ": " + msg.DLC.ToString() + " " + transmitter;
                sw.WriteLine(line);

                foreach(DbcItem sig in msg.Items)
                {
                    string mode = "";
                    if (sig.Type == DBCSignalType.Mode)
                        mode = " M";
                    if (sig.Type == DBCSignalType.ModeDependent)
                        mode = " m" + sig.Mode;

                    string byteorder = sig.ByteOrder == DBCByteOrder.Intel ? "1" : "0";
                    string datatype = sig.ValueType == DBCValueType.Unsigned ? "+" : "-";
                    string receivers = "Vector__XXX";
                    line = "\tSG_ " + sig.CleanName + " " + mode + " : " + sig.StartBit.ToString() + "|" + sig.BitCount.ToString() + "@" + byteorder + datatype +
                        " (" + sig.Factor.ToString() + "," + sig.Offset.ToString() + ") [" + sig.MinValue.ToString() + "|" + sig.MaxValue.ToString() +
                        "] " + Convert.ToChar(34) + sig.Units + Convert.ToChar(34) + " " + receivers;

                    sw.WriteLine(line);
                }
            }
        }

        private void WriteComments(StreamWriter sw, List<DbcMessage> messages)
        {
            sw.WriteLine("");

            string line;
            foreach (DbcMessage msg in messages)
            {
                if ((msg.Comment != null) && (msg.Comment != ""))
                {
                    line = "CM_ BO_ " + msg.CANID.ToString() + " " + Convert.ToChar(34) + msg.Comment + Convert.ToChar(34) + ";";
                    sw.WriteLine(line);
                }

                foreach (DbcItem sig in msg.Items)               
                    if ((sig.Comment != null) && (sig.Comment != ""))
                    {
                        line = "CM_ SG_ " + msg.CANID.ToString() + " " + sig.CleanName + " " + Convert.ToChar(34) + sig.Comment + Convert.ToChar(34) + ";";
                        sw.WriteLine(line);
                    }                
            }
        }

        private void WriteEnumValues(StreamWriter sw, List<DbcMessage> messages)
        {
            sw.WriteLine("");

            string line;
            foreach (DbcMessage msg in messages)            
                foreach (DbcItem sig in msg.Items)                
                    if (sig.Conversion.Type == ConversionType.FormulaAndTableVerbal)                
                    {                    
                        line = "VAL_ " + msg.CANID.ToString() + " " + sig.CleanName;
                    
                        foreach (KeyValuePair<double, string> pair in sig.Conversion.TableVerbal.Pairs   )                                         
                            line = line + " " + pair.Key.ToString() + " " + Convert.ToChar(34) + pair.Value + Convert.ToChar(34);
                        line = line + ";";

                        sw.WriteLine(line);                
                    }               
        }

        private void SaveDBCToFile(List<DbcMessage> messages)
        {
            using (StreamWriter sw = new StreamWriter(FileName))
            {
                sw.WriteLine(@"VERSION """"");
                sw.WriteLine("");
                sw.WriteLine("NS_:");
                sw.WriteLine("");
                sw.WriteLine("BS_:");
                sw.WriteLine("");
                sw.WriteLine("BU_: Vector__XXX");

                WriteMessages(sw, messages);
                WriteComments(sw, messages);
                WriteEnumValues(sw, messages);
                //sw.Close();
            }
        }
        public void ExportSelected(List<DbcItem> selected)
        {
            if (FileName == "")
                return;

            if (selected.Count == 0)
                return;

            // export selected DbcItems to DbcMessage list like multiplexed signals
            List<DbcMessage> Messages = new List<DbcMessage>();
            DbcMessage msg = new DbcMessage();
            msg.Name = "Service22MUXSignals";
            msg.DLC = 8;
            msg.Transmitter = "Vector__XXX";
            msg.CANID = 0x7e8;  // must be user defined
            msg.Comment = "Influx_Service_0x22_Items_To_MUX_Signals";

            DbcItem sig = new DbcItem();
            sig.Name = "Service22SELECTOR";
            sig.Type = DBCSignalType.Mode;
            sig.StartBit = 15;
            sig.BitCount = 24;
            sig.ByteOrder = DBCByteOrder.Motorola;
            sig.ValueType = DBCValueType.Unsigned;
            sig.Conversion.Formula.CoeffB = 1;
            sig.Conversion.Formula.CoeffC = 0;
            sig.MinValue = 0x620001;
            sig.MaxValue = 0x62FFFF;
            sig.Comment = "Service_0x22_ID_SELECTOR";

            msg.Items.Add(sig);

            foreach (DbcItem item in selected)
            {
                sig = item.Clone;                
                sig.Type = DBCSignalType.ModeDependent;
                sig.Mode = (0x62 << 16) + (UInt32)sig.Ident;                    
                sig.Ident = msg.CANID;
                sig.StartBit = (ushort)(sig.StartBit + 32);

                msg.Items.Add(sig);
            }            

            Messages.Add(msg);

            SaveDBCToFile(Messages);
        }
        public DBC()
        {
            Messages = new List<DbcMessage>();
        }

        public void AddToReferenceCollection(ReferenceCollection collection, byte BusChannel)
        {
            foreach (var msg in Messages)
                foreach (var sig in msg.Items)
                    collection.Add(new ReferenceDbcChannel()
                    {
                        BusChannelIndex = BusChannel,
                        FileName = FileNameSerialized,
                        MessageID = msg.FullID,
                        SignalName = sig.Name
                    });
        }
    }

    public class ExportDbcMessage
    {
        public UInt64 uniqueid { get; set; }
        public byte BusChannel { get; set; }
        public DbcMessage Message { get; set; }
        public List<DbcItem> Signals { get; set; }

        public DbcItem multiplexor = null;
        public BinaryData multiplexorData = null;
        // Multiplexor map is dictionary with pair - mode value and list of signal indexes
        public Dictionary<UInt16, List<UInt16>> multiplexorMap = null;
        // Mode dependant group ids
        public List<UInt64> multiplexorGroups = null;

        public static bool operator ==(ExportDbcMessage item1, ExportDbcMessage item2) => item1.BusChannel == item2.BusChannel && item1.Message == item2.Message;
        public static bool operator !=(ExportDbcMessage item1, ExportDbcMessage item2) => !(item1 == item2);
        public override bool Equals(object obj)
        {
            if (obj is ExportDbcMessage)
                return this == (ExportDbcMessage)obj;
            else
                return false;
        }

        public void AddSignal(DbcItem Signal)
        {
            Signals.Add(Signal);
            multiplexor = GetMode();
            if (multiplexor is not null)
                multiplexorData = multiplexor.GetDescriptor.CreateBinaryData();
        }

        public DbcItem GetMode()
        {
            if (Signals[0].Type == DBCSignalType.Mode)
            {
                multiplexorMap =
                    Signals.Where(sg => sg.Type == DBCSignalType.ModeDependent).
                    GroupBy(md => md.Mode,
                    (k, c) => new { ModeValue = (UInt16)k, Indexes = c.Select(cs => (UInt16)Signals.IndexOf(cs)).ToList() }).
                    ToDictionary(d => d.ModeValue, d => d.Indexes);

                return Signals[0];
            }
            else
            {
                DbcItem mode = Signals.FirstOrDefault(s => s.Type == DBCSignalType.Mode);
                if (mode is not null)
                {
                    Signals.Remove(mode);
                    Signals.Insert(0, mode);

                    multiplexorMap =
                        Signals.Where(sg => sg.Type == DBCSignalType.ModeDependent).
                        GroupBy(md => md.Mode,
                        (k, c) => new { ModeValue = (UInt16)k, Indexes = c.Select(cs => (UInt16)Signals.IndexOf(cs)).ToList() }).
                        ToDictionary(d => d.ModeValue, d => d.Indexes);
                }
                return mode;
            }
        }

        public UInt16 FindMultiplexorIndex(DbcItem signal)
        {
            var keys = multiplexorMap.Where(pair => pair.Value.Contains((ushort)Signals.IndexOf(signal))).
                Select(pair => pair.Key);

            if (keys.Count() > 0)
                return (UInt16)multiplexorMap.Keys.ToList().IndexOf(keys.First());
            else
                return UInt16.MaxValue;
        }

        public UInt32 CalcInvalidationBitCount => (uint)(multiplexorMap is null ? 0 : (multiplexorMap.Count() + 7) >> 3);
    }

    public class ExportDbcCollection : List<ExportDbcMessage>
    {
        public ExportDbcMessage AddMessage(byte BusChannel, DbcMessage Message)
        {
            foreach (ExportDbcMessage m in this)
                if (m.BusChannel == BusChannel && m.Message == Message)
                    return m;

            ExportDbcMessage channel = new ExportDbcMessage()
            {
                BusChannel = BusChannel,
                Message = Message,
                Signals = new List<DbcItem>()
            };
            Add(channel);
            return channel;
        }

    }

    public static class DbcHelper
    {
        public static string ToDisplayName(this DBCMessageType msgType)
        {
            switch (msgType)
            {
                case DBCMessageType.Standard: return "CAN Standard";
                case DBCMessageType.Extended: return "CAN Extended";
                case DBCMessageType.CanFDStandard: return "CAN FD Standard";
                case DBCMessageType.CanFDExtended: return "CAN FD Extended";
                case DBCMessageType.J1939PG: return "J1939 PG (ext. ID)";
                case DBCMessageType.Lin: return "Lin";
                case DBCMessageType.KanCan: return "KanCan";
                default: return "Unknown";
            }
        }
    }
}
