using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinVariableAggregation : BinBase
    {
        internal enum BinProp
        {
            InputUID,
            DefaultValue,
            NewValAggregation,
            NewValCondition,
            NewValTimeout,
        }

        #region Do not touch these
        public BinVariableAggregation(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.DefaultValue, typeof(Single));
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
                data.AddProperty(BinProp.NewValCondition, typeof(byte));
                data.AddProperty(BinProp.NewValTimeout, typeof(UInt16));
                data.AddProperty(BinProp.NewValAggregation, typeof(byte));
            });
        }

    }
}
