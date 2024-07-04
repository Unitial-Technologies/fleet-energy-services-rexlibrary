namespace InfluxShared.FileObjects
{
    public class BasicItemInfo
    {
        public string Name { get; set; }

        public string Units { get; set; } = string.Empty;

        public virtual uint Ident { get; set; }
        public virtual string IdentHex => "0x" + Ident.ToString("X4");

        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public byte ItemType { get; set; } //0: DBC; 1: A2L; 
        public string Comment { get; set; }
        public ItemConversion Conversion { get; set; }
        public bool IsSnapshot { get; set; }

        public BasicItemInfo()
        {
            Conversion = new ItemConversion();
        }

        public virtual ChannelDescriptor GetDescriptor => null;
        public object TempObj = null;
    }
}
