using InfluxShared.FileObjects;
using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    public enum DigitalType : byte
    {
        DIGITAL,
        FREQUENCY,
        IMPULSE,
        PWM
    }

    public enum DigitalActiveState : byte
    {
        LOW,
        HI
    }
    #endregion

    class BinDigitalIn : BinBase
    {
        internal enum BinProp
        {
            PhysicalNumber,
            SamplingRate,
            DigitalType,
            ActiveState,
        }

        #region Do not touch these
        public BinDigitalIn(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override string GetName => "Digital " + this[BinDigitalIn.BinProp.PhysicalNumber].ToString();
        internal override ChannelDescriptor GetDataDescriptor => new ChannelDescriptor()
        { StartBit = 0, BitCount = 8, isIntel = true, HexType = typeof(UInt64), conversionType = ConversionType.None, Name = GetName, Units = GetUnits };

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.SamplingRate, typeof(UInt16), DefaultValue: 100);
                data.AddProperty(BinProp.DigitalType, typeof(DigitalType));
                data.AddProperty(BinProp.ActiveState, typeof(DigitalActiveState));
            });
            
            AddOutput("UID");
        }
    }
}