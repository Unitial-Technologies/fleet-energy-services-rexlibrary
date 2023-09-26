using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    enum ArithmeticType : byte
    {
        SUM,
        SUB,
        MUL,
        DIV,
        AND,
        OR,
    }
    #endregion

    class BinArithmetic : BinBase
    {
        internal enum BinProp
        {
            Input1UID,
            Input2UID,
            ArithmeticAction
        }

        #region Do not touch these
        public BinArithmetic(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.Input1UID, typeof(UInt16));
                data.AddProperty(BinProp.Input2UID, typeof(UInt16));
            });
            Versions[2] = new Action(() =>
            {
                Versions[1].DynamicInvoke();
                data.AddProperty(BinProp.ArithmeticAction, typeof(ArithmeticType));
            });

            AddInput(BinProp.Input1UID.ToString());
            AddInput(BinProp.Input2UID.ToString());
            AddOutput("UID");
        }

    }
}
