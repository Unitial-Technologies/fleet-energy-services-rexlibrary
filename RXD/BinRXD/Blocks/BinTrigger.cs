using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions

    #endregion

    class BinTrigger : BinBase
    {
        internal enum BinProp
        {
            KeepActiveTime,
            DoNotActivateTimeout,
            InputUID1,
            InputUID2,
            OperatorCondition,
            NameSize,
            Name,
        }

        #region Do not touch these
        public BinTrigger(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.KeepActiveTime, typeof(UInt32));
                data.AddProperty(BinProp.DoNotActivateTimeout, typeof(UInt32));
                data.AddProperty(BinProp.InputUID1, typeof(UInt16)); 
                data.AddProperty(BinProp.InputUID2, typeof(UInt16)); 
                data.AddProperty(BinProp.OperatorCondition, typeof(ConditionType));
            });
            Versions[2] = new Action(() =>
            {
                Versions[1].DynamicInvoke();
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
            });
        }

    }
}
