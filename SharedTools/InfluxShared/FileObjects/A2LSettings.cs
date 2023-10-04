using System;
using System.Collections.Generic;
using System.Text;
using InfluxShared.Helpers;

namespace InfluxShared.FileObjects
{
    public class A2LSettings
    {
        public string Name { get; set; } = "";
        public uint Cro { get; set; }   
        public uint Dto { get; set; }        
        public ushort StationAddress { get; set; }
        public string CroHex
        {
            get => "0x" + Cro.ToString("X2");
            set => Cro = value.ConvertFromHex();
        }
        public string DtoHex
        {
            get => "0x" + Dto.ToString("X2");
            set => Dto = value.ConvertFromHex();
        }
        public string StationAddressHex
        {
            get => "0x" + StationAddress.ToString("X2");
            set => StationAddress = (ushort)value.ConvertFromHex();
        }
        public byte ByteOrder { get; set; }
        public uint Baudrate { get; set; }
        public uint BaudrateFD { get; set; }
        public byte RateIndex { get => GetRateIndex(); set => SetRate(value); }
        public ushort OdtSize { get;set; }
        public ushort OdtEntrySize { get; set; }
        public ushort OdtCount { get => (ushort)Odts.Count; }
        public List<A2lOdt> Odts { get; set; }


        public A2LSettings()
        {
            Odts = new List<A2lOdt>();
        }

        private byte GetRateIndex()
        {
            if (Baudrate < 126000)
                return 4;
            else if (Baudrate <255000)
                return 3;
            else if (Baudrate < 501000)
                return 2;
            else if (Baudrate < 751000)
                return 1;
            else
                return 0;
        }

        private void SetRate(byte index)
        {
            if (index == 0)
                Baudrate = 1000000;
            else if (index == 1)
                Baudrate = 750000;
            else if (index == 2)
                Baudrate = 500000;
            else if (index == 3)
                Baudrate = 250000;
            else
                Baudrate = 125000;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
