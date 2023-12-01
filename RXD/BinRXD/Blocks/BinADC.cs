using InfluxShared.FileObjects;
using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    internal class BinADC : BinBase
    {
        internal enum BinProp
        {
            PhysicalNumber,
            Rate,
            ParA,
            ParB,
        }

        #region Do not touch these
        public BinADC(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override string GetName => "ADC " + this[BinADC.BinProp.PhysicalNumber].ToString();
        internal override string GetUnits => "Volt";
        internal override ChannelDescriptor GetDataDescriptor => new ChannelDescriptor()
        { StartBit = 0, BitCount = 32, isIntel = true, HexType = typeof(Single), conversionType = ConversionType.None, Name = GetName, Units = GetUnits };

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.Rate, typeof(UInt16));
                data.AddProperty(BinProp.ParA, typeof(Single));
                data.AddProperty(BinProp.ParB, typeof(Single));
            });
            AddOutput("UID");
        }
    }
}