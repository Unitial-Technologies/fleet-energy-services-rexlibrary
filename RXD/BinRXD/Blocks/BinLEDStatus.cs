using System;

namespace RXD.Blocks
{
    public class BinLEDStatus : BinBase
    {
        internal enum LED_Assign_Option : byte
        {
            DEFAULT,
            MODEL
        }

        internal enum LED_DisplayOption : byte
        {
            OFF,
            ON,
            NORMAL,
            FAST,
            SLOW
        }

        internal enum BinProp
        {
            PhysicalNumber,
            AssignOption,
            InputUIDActive,
            InputUIDDeactive,
            ActiveTimeout,
            DeactiveTimeout,
            WhenActive,
            WhenDeactive,
            WhenNotActive,
        }


        #region Do not touch these
        public BinLEDStatus(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.AssignOption, typeof(LED_Assign_Option));
                data.AddProperty(BinProp.InputUIDActive, typeof(UInt16));
                data.AddProperty(BinProp.InputUIDDeactive, typeof(UInt16));
                data.AddProperty(BinProp.ActiveTimeout, typeof(UInt32));
                data.AddProperty(BinProp.DeactiveTimeout, typeof(UInt32));
                data.AddProperty(BinProp.WhenActive, typeof(LED_DisplayOption));
                data.AddProperty(BinProp.WhenDeactive, typeof(LED_DisplayOption));
                data.AddProperty(BinProp.WhenNotActive, typeof(LED_DisplayOption));
            });
            AddInput(BinProp.InputUIDActive.ToString());
            AddInput(BinProp.InputUIDDeactive.ToString());
            AddOutput("UID");
        }
    }
}
