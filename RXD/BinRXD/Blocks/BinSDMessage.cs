using System;
using System.Collections.Generic;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    #endregion

    class BinSDMessage : BinBase
    {
        internal enum BinProp
        {
            MessageIdentStart,
            MessageIdentEnd,
            Direction,
            DLC,
            IsExtended,
            InterfaceUID,
            Period,
            TriggeringMessageUniqueID,
            DefaultHex,
            InputMessageUID,            
        }

        #region Do not touch these
        public BinSDMessage(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.MessageIdentStart, typeof(UInt32));
                data.AddProperty(BinProp.MessageIdentEnd, typeof(UInt32), DefaultValue: 0xFFFFFFFF);
                data.AddProperty(BinProp.Direction, typeof(DirectionType), DefaultValue: DirectionType.OutputPeriodic);
                data.AddProperty(BinProp.DLC, typeof(byte), DefaultValue: 8);
                data.AddProperty(BinProp.IsExtended, typeof(bool)); // bool
                data.AddProperty(BinProp.InterfaceUID, typeof(UInt16));
                data.AddProperty(BinProp.Period, typeof(UInt32));
                data.AddProperty(BinProp.TriggeringMessageUniqueID, typeof(UInt16));
                data.AddProperty(BinProp.DefaultHex, typeof(byte[]), BinProp.DLC);
                data.AddProperty(BinProp.InputMessageUID, typeof(UInt16));
            });
           /* Versions[2] = new Action(() =>
            {
                Versions[1].DynamicInvoke();
                data.AddProperty(BinProp.Downsampling, typeof(UInt32));
            });*/

            AddInput(BinProp.InputMessageUID.ToString());
            AddOutput(BinProp.InterfaceUID.ToString());
        }
    }
}
