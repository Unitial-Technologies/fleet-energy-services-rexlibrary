using InfluxShared.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace InfluxShared.FileObjects
{
    public class A2lOdt
    {
        public string Name { get; set; }
        public uint Ident { get; set; }
        public byte Prescale { get; set; }
        public byte Size { get; set; }
        public short Start { get; set; }
        public byte EventChannel { get; set; }
        public string IdentHex
        {
            get => "0x" + Ident.ToString("X2");
            set => Ident = value.ConvertFromHex();
        }
    }
}
