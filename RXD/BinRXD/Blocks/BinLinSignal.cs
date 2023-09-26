using InfluxShared.FileObjects;
using InfluxShared.Objects;
using System;

namespace RXD.Blocks
{

    class BinLinSignal : BinBase
    {
        internal enum BinProp
        {            
            InputUID,
            MessageUID,
            StartBit,
            BitCount,
            Endian,
            DefaultValue,
            ParA,
            ParB,
            SignalType,
            NameSize,
            Name,
        }

        #region Do not touch these
        public BinLinSignal(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override ChannelDescriptor GetDataDescriptor => new ChannelDescriptor()
        {
            StartBit = this[BinProp.StartBit],
            BitCount = this[BinProp.BitCount],
            isIntel = this[BinProp.Endian] == SignalByteOrder.INTEL,
            HexType = BinaryData.BinaryTypes[(int)this[BinProp.SignalType]],
            conversionType = ConversionType.Formula,
            Factor = this[BinProp.ParA],
            Offset = this[BinProp.ParB],
            Name = GetName,
            Units = GetUnits
        };

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
                data.AddProperty(BinProp.MessageUID, typeof(UInt16));
                data.AddProperty(BinProp.StartBit, typeof(UInt16));
                data.AddProperty(BinProp.BitCount, typeof(byte));
                data.AddProperty(BinProp.Endian, typeof(SignalByteOrder));
                data.AddProperty(BinProp.DefaultValue, typeof(Single));
                data.AddProperty(BinProp.ParA, typeof(Single));
                data.AddProperty(BinProp.ParB, typeof(Single));
                data.AddProperty(BinProp.SignalType, typeof(SignalDataType));
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
            });

            AddInput(BinProp.InputUID.ToString());
            AddInput(BinProp.MessageUID.ToString());
            AddOutput("UID");
        }

    }
}
