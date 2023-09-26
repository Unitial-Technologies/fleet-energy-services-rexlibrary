using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinCounter : BinBase
    {
        internal enum BinProp
        {
            Seed,
            Cycle,
            MaxValue,
            InitialValue,
        }

        #region Do not touch these
        public BinCounter(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override void SetupVersions()
        {
            Versions[2] = new Action(() =>
            {
                data.AddProperty(BinProp.Seed, typeof(UInt16));
                data.AddProperty(BinProp.Cycle, typeof(UInt16));
                data.AddProperty(BinProp.MaxValue, typeof(UInt32));
                data.AddProperty(BinProp.InitialValue, typeof(UInt32));
            });

            AddOutput("UID");
        }
    }
}
