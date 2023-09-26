using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinTimer : BinBase
    {
        internal enum BinProp
        {
            AutoEnable,
            InitialEnable,
            DisableUID,
            EnableUID,
            Timeout,
        }

        #region Do not touch these
        public BinTimer(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.Timeout, typeof(Int32));
                data.AddProperty(BinProp.AutoEnable, typeof(bool)); // bool
                data.AddProperty(BinProp.InitialEnable, typeof(bool)); // bool
                data.AddProperty(BinProp.EnableUID, typeof(UInt16));
                data.AddProperty(BinProp.DisableUID, typeof(UInt16));
            });
            AddInput(BinProp.EnableUID.ToString());
            AddInput(BinProp.DisableUID.ToString());
            AddOutput("UID");
        }

    }
}
