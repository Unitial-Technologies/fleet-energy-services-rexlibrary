using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinRationalFormula : BinBase
    {
        internal enum BinProp
        {
            InputUID,
            ParA,
            ParB,
            ParC,
            ParD,
            ParE,
            ParF,
        }

        #region Do not touch these
        public BinRationalFormula(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
                data.AddProperty(BinProp.ParA, typeof(Single));
                data.AddProperty(BinProp.ParB, typeof(Single));
                data.AddProperty(BinProp.ParC, typeof(Single));
                data.AddProperty(BinProp.ParD, typeof(Single));
                data.AddProperty(BinProp.ParE, typeof(Single));
                data.AddProperty(BinProp.ParF, typeof(Single));
            });

            AddInput(BinProp.InputUID.ToString());
            AddOutput("UID");
        }

    }
}
