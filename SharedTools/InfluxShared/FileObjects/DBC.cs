using InfluxShared.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace InfluxShared.FileObjects
{
    public enum DBCByteOrder : byte { Intel, Motorola }
    public enum DBCMessageType : byte { Standard, Extended, CanFDStandard, CanFDExtended, J1939PG, Lin }
    public enum DBCValueType : byte { Unsigned, Signed, IEEEFloat, IEEEDouble }
    public enum DBCSignalType : byte { Standard, Mode, ModeDependent }
    public enum DBCFileType : byte { None, Generic, CAN, CANFD, LIN, J1939, Ethernet, FlexRay };


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
        public byte Mode { get; set; }   //If the signal is Mode Dependent
        public DBCByteOrder ByteOrder { get; set; }
        public DBCValueType ValueType { get; set; }
        public bool Log { get; set; }
        public override string ToString()
        {
            return Name;
        }
        public double Factor => Conversion.Formula.CoeffB;
        public double Offset => Conversion.Formula.CoeffC;

        public static bool operator ==(DbcItem item1, DbcItem item2) =>
            item1.StartBit == item2.StartBit &&
            item1.BitCount == item2.BitCount &&
            item1.Type == item2.Type &&
            item1.Mode == item2.Mode &&
            item1.ByteOrder == item2.ByteOrder &&
            item1.ValueType == item2.ValueType;
        public static bool operator !=(DbcItem item1, DbcItem item2) => !(item1 == item2);
        public override bool Equals(object obj) 
        {
            if (obj is DbcItem)
                return this == (DbcItem)obj;
            else
                return false;
        }

        public override ChannelDescriptor GetDescriptor => new ChannelDescriptor()
        {
            StartBit = StartBit,
            BitCount = BitCount,
            isIntel = ByteOrder == DBCByteOrder.Intel,
            HexType = BinaryData.BinaryTypes[(int)ValueType],
            Factor = Factor,
            Offset = Offset,
            Name = Name,
            Units = Units
        };
    }

    public class DbcMessage
    {
        public string Name { get; set; }
        public uint CANID { get; set; }
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
        }
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
                default: return "Unknown";
            }
        }
    }
}
