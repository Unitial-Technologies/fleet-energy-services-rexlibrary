using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinSwitch : BinBase
    {
        internal enum BinProp
        {
            InputSwitchUID,
            InputDefaultUID,
            InputCount,
            InputUID,
            CaseValue,
        }

        #region Do not touch these
        public BinSwitch(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.InputSwitchUID, typeof(UInt16));
                data.AddProperty(BinProp.InputDefaultUID, typeof(UInt16));
                data.AddProperty(BinProp.InputCount, typeof(byte));
                data.AddProperty(BinProp.InputUID, typeof(UInt16[]), BinProp.InputCount);
                data.AddProperty(BinProp.CaseValue, typeof(Int32[]), BinProp.InputCount);
            });
        }

    }
}
