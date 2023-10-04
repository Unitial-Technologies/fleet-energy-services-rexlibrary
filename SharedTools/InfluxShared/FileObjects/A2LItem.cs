using System;
using System.Collections.Generic;
using System.Text;

namespace InfluxShared.FileObjects
{
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
        public bool Selected { get; set; }
    }
}
