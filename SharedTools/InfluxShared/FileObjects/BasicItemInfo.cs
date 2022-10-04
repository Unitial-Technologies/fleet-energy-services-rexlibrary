namespace InfluxShared.FileObjects
{
    public class BasicItemInfo
    {
        public string Name { get; set; }
        public string Units { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public byte ItemType { get; set; } //0: DBC; 1: A2L; 
        public string Comment { get; set; }
        public ItemConversion Conversion { get; set; }

        public BasicItemInfo()
        {
            Conversion = new ItemConversion();
        }

        public virtual ChannelDescriptor GetDescriptor => null;
    }


}
