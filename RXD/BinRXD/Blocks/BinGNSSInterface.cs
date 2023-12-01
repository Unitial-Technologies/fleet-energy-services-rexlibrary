using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Blocks
{
    internal class BinGNSSInterface : BinBase
    {
        internal enum BinProp
        {
            PhysicalNumber,
            SamplingRate,
        }

        #region Do not touch these
        public BinGNSSInterface(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.SamplingRate, typeof(UInt16));
                AddOutput("");
            });
        }
    }
}
