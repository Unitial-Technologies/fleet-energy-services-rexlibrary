using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Blocks
{
    internal class BinTC : BinBase
    {
        internal enum TcType : byte
        {
            K,
            J,
            N,
            R,
            S,
            B,
            T,
            E
        }
        internal enum TcMode : byte
        {
            Normal,
            NoCalibration,
            NoCJC,
            RAW,
            OnlyCJC
        }
        internal enum BinProp
        {
            PhysicalNumber,
            Type,
            Mode,
            Rate,
            ParA,
            ParB,
        }

        #region Do not touch these
        public BinTC(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        public override string GetName => "TC " + this[BinTC.BinProp.PhysicalNumber].ToString();
        public override string GetUnits => "Volt";
        public override ChannelDescriptor GetDataDescriptor => new ChannelDescriptor()
        { StartBit = 0, BitCount = 32, isIntel = true, HexType = typeof(Single), conversionType = ConversionType.None, Name = GetName, Units = GetUnits };

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.Type, typeof(TcType));
                data.AddProperty(BinProp.Mode, typeof(TcMode));
                data.AddProperty(BinProp.Rate, typeof(UInt16));
                data.AddProperty(BinProp.ParA, typeof(Single));
                data.AddProperty(BinProp.ParB, typeof(Single));
            });
            AddOutput("UID");
        }
    }
}
