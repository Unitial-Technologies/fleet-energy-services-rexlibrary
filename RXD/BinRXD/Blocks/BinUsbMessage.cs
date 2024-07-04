using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    internal class BinUsbMessage : BinBase
    {
        internal enum BinProp
        {
            MessageIdentStart,
            MessageIdentEnd,
            Direction,
            DLC,
            IsExtended,
            InterfaceUID,
            Period,
            DefaultHex,
            InputUID,
        }

        #region Do not touch these
        public BinUsbMessage(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.MessageIdentStart, typeof(UInt32));
                data.AddProperty(BinProp.MessageIdentEnd, typeof(UInt32), DefaultValue: 0xFFFFFFFF);
                data.AddProperty(BinProp.Direction, typeof(DirectionType), DirectionType.OutputPeriodic);
                data.AddProperty(BinProp.DLC, typeof(byte), DefaultValue: 8);
                data.AddProperty(BinProp.IsExtended, typeof(bool)); // bool
                data.AddProperty(BinProp.InterfaceUID, typeof(UInt16));
                data.AddProperty(BinProp.Period, typeof(UInt32));
                data.AddProperty(BinProp.DefaultHex, typeof(byte[]), BinProp.DLC);
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
            });
            AddInput(BinProp.InputUID.ToString());
            AddOutput(BinProp.InterfaceUID.ToString());
        }

    }
}
