using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinUsbInterface : BinBase
    {
        internal enum BinProp
        {
        }

        #region Do not touch these
        public BinUsbInterface(BinHeader hs = null) : base(hs) { }

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
            });
        }

    }
}
