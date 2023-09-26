using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Blocks
{
    public class BinLinMessage : BinBase
    {
        internal enum BinProp
        {
            InterfaceID,
            MessageIdentStart,
            MessageIdentEnd,
            Respond,
            DLC,
            DefaultHex,
            Delay,
            ScheduleTablePosition,
            InputUID,
            NameSize,
            Name
        }


        #region Do not touch these
        public BinLinMessage(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.InterfaceID, typeof(UInt16));
                data.AddProperty(BinProp.MessageIdentStart, typeof(byte), DefaultValue: 0);
                data.AddProperty(BinProp.MessageIdentEnd, typeof(byte), DefaultValue: 64);
                data.AddProperty(BinProp.Respond, typeof(byte));
                data.AddProperty(BinProp.DLC, typeof(byte), DefaultValue: 3);
                data.AddProperty(BinProp.DefaultHex, typeof(byte[]), BinProp.DLC);
                data.AddProperty(BinProp.Delay, typeof(UInt16));
                data.AddProperty(BinProp.ScheduleTablePosition, typeof(UInt16));
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
            });
            AddInput(BinProp.InputUID.ToString());
            AddInput(BinProp.InterfaceID.ToString());
            AddOutput("UID");
        }
    }
}
