using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinConstant : BinBase
    {
        internal enum BinProp
        {
            Value,
        }

        #region Do not touch these
        public BinConstant(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.Value, typeof(Single));
            });
            AddOutput("UID");
        }
    }
}
