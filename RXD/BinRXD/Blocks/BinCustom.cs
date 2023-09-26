using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinCustom : BinBase
    {
        internal enum BinProp
        {
            CustomID,
            CustomCodeVersion,
            InputCount,
            InputUID,
        }
               

        #region Do not touch these
        public BinCustom(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.CustomID, typeof(UInt16));
                data.AddProperty(BinProp.CustomCodeVersion, typeof(UInt16));
                data.AddProperty(BinProp.InputCount, typeof(byte));
                data.AddProperty(BinProp.InputUID, typeof(UInt16[]), BinProp.InputCount);
            });

            //AddInput(BinProp.InputUID.ToString());
           // AddOutput("UID");
        }

    }
}
