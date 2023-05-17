using System;
using System.Collections.Generic;
using System.IO;

namespace InfluxShared.FileObjects
{
    public enum A2LItemType : byte { Measurement }
    public enum A2LValueТype: byte { UnsignedByte, SignedByte, UnsignedWord, SignedWord, UnsignedLong, SignedLong, IEEEFloat, IEEEDouble }
    public enum A2LByteOrder: byte { Intel, Motorola }  // MSBLast = Intel = Little Endian

    public class A2L
    {
        public string FileName { get; set; }
        public string FileNameSerialized { get; set; }
        public string FileNameNoExt => Path.GetFileNameWithoutExtension(FileName);
        public string FileLocation => Path.GetDirectoryName(FileName);
        public List<A2LItem> Items { get; set; }

        public A2L() 
        {
            Items = new List<A2LItem>();
        }

        public void AddToReferenceCollection(ReferenceCollection collection, byte BusChannel)
        {
            foreach (var item in Items)
                collection.Add(new ReferenceA2LChannel()
                {
                    BusChannelIndex = BusChannel,
                    FileName = FileNameSerialized,
                    Address = item.Address,
                    SignalName = item.Name
                });
        }
    }

    public class A2LItem : BasicItemInfo
    {
        public A2LItem()
        {
            
        }

        public uint Address { get; set; }
        public A2LItemType MsgType { get; set; }
        public A2LValueТype Datatype { get; set; }
        public A2LByteOrder ByteOrder { get; set; }
        public byte ShLeft { get; set; }
        public byte ShRight { get; set; }
        public uint BitMask { get; set; }
        public string AddressHex { get => "0x" + Address.ToString("X4"); }
    }

    public class ExportA2LItem
    {
        public UInt64 uniqueid { get; set; }
        public byte BusChannel { get; set; }
        public A2LItem Item { get; set; }

        public static bool operator ==(ExportA2LItem item1, ExportA2LItem item2) => item1.BusChannel == item2.BusChannel && item1.Item == item2.Item;
        public static bool operator !=(ExportA2LItem item1, ExportA2LItem item2) => !(item1 == item2);
        public override bool Equals(object obj)
        {
            if (obj is ExportA2LItem)
                return this == (ExportA2LItem)obj;
            else
                return false;
        }
    }

    public class ExportA2LCollection : List<ExportA2LItem>
    {
        public ExportA2LItem AddItem(byte BusChannel, A2LItem Item)
        {
            foreach (ExportA2LItem m in this)
                if (m.BusChannel == BusChannel && m.Item == Item)
                    return m;

            ExportA2LItem channel = new ExportA2LItem()
            {
                BusChannel = BusChannel,
                Item = Item,
            };
            Add(channel);
            return channel;
        }
    }

    public static class A2LHelper
    {
        public static string ToDisplayName(this A2LValueТype valType)
        {
            switch (valType)
            {
                case A2LValueТype.UnsignedByte: return "Unsigned byte";
                case A2LValueТype.SignedByte: return "Signed byte";
                case A2LValueТype.UnsignedWord: return "Unsigned word";
                case A2LValueТype.SignedWord: return "Signed word";
                case A2LValueТype.UnsignedLong: return "Unsigned long";
                case A2LValueТype.SignedLong: return "Signed long";
                case A2LValueТype.IEEEFloat: return "IEEE Float";
                case A2LValueТype.IEEEDouble: return "IEEE Double";
                default: return "Unknown";
            }
        }
    }

}
