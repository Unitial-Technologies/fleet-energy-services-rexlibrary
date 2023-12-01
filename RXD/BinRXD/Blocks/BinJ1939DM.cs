using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    internal enum DMTypeEnum : byte
    {
        DM1 = 1
    }
    #endregion

    internal class BinJ1939DM : BinBase
    {
        internal enum BinProp
        {
            DMType,
            InputUID,
            Source,
        }


        #region Do not touch these
        public BinJ1939DM(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.DMType, typeof(DMTypeEnum));
                data.AddProperty(BinProp.Source, typeof(byte));
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
            });
            AddInput(BinProp.InputUID.ToString());
            AddOutput("UID");
        }
    }
}