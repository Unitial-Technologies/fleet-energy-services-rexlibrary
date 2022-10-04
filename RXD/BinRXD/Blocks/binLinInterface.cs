using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Blocks
{
    public class BinLinInterface : BinBase

    {
        internal enum BinProp
        {
            PhysicalNumber,
            MasterMode,
            BitRate,
            ProtocolVersion,
            Time_Base,
            Time_Jitter,            
        }

        internal enum LINProtocolVersion : byte
        {
            V_1_3,
            V_2_0
        }

        #region Do not touch these
        public BinLinInterface(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.MasterMode, typeof(byte));
                data.AddProperty(BinProp.BitRate, typeof(UInt16));
                data.AddProperty(BinProp.ProtocolVersion, typeof(LINProtocolVersion));
                data.AddProperty(BinProp.Time_Base, typeof(UInt16));
                data.AddProperty(BinProp.Time_Jitter, typeof(UInt16)); 
            });            
        }
    }
}
