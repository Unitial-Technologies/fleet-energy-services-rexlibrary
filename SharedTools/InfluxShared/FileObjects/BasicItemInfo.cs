using System.Text.RegularExpressions;

namespace InfluxShared.FileObjects
{
    public class BasicItemInfo
    {
        private Regex BadCharacters = new Regex("[^a-zA-Z0-9_]");
        public string CleanName { get => BadCharacters.Replace(Name, ""); }

        public string Name { get; set; }
        public string Units { get; set; }
        public uint Ident { get; set; }
        public string IdentHex { get => "0x" + Ident.ToString("X4"); }
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
    }


}
