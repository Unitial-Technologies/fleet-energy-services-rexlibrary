using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    internal class BinDigitalOut : BinBase
    {
        internal enum BinProp
        {
            PhysicalNumber,
            DigitalType,
            PWMFrequency,
            ActiveState,
            InputUID,
        }

        #region Do not touch these
        public BinDigitalOut(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.DigitalType, typeof(DigitalType));
                data.AddProperty(BinProp.PWMFrequency, typeof(UInt32));
                data.AddProperty(BinProp.ActiveState, typeof(DigitalActiveState));
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
            });
            AddInput(BinProp.InputUID.ToString());
            AddOutput("UID");
        }
    }
}