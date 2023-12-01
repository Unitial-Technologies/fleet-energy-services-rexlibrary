using System;

namespace RXD.Blocks
{
    internal class BinCanError : BinBase
    {
        internal enum BinProp
        {
            InterfaceID,
        }
        #region Do not touch these
        public BinCanError(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.InterfaceID, typeof(UInt16));
            });
            AddInput(BinProp.InterfaceID.ToString());
        }
    }
}