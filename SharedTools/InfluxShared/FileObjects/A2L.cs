using System.Collections.Generic;

namespace InfluxShared.FileObjects
{
    public enum A2LDatatype{ UByte, SByte, UWord, SWord, ULong, SLong, Float32, Float64};
    public enum A2LByteOrder { MsbLast, MsbFirst};  // MSBLast = Intel = Little Endian
    public class A2L
    {
        public A2L() 
        {
            Items = new List<A2LItem>();
        } 
        public string FileName { get; set; }
        public string FileNameSerialized { get; set; }
        public List<A2LItem> Items { get; set; }
    }

    public class A2LItem : BasicItemInfo
    {
        public A2LItem()
        {
            
        }
        public uint Address { get; set; }
        public A2LDatatype Datatype { get; set; }
        public A2LByteOrder ByteOrder { get; set; }
        public byte ShLeft { get; set; }
        public byte ShRight { get; set; }
        public uint BitMask { get; set; }
        public string AddressHex { get => "0x" + Address.ToString("X4"); }
    }
}
