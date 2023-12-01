using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    public enum CanType : byte
    {
        CAN,
        CAN_FD
    }

    internal enum WakeUpMode : byte
    {
        NO_WAKE_UP,
        WAKE_UP
    }

    internal enum SleepMode : byte
    {
        NO_SLEEP,
        SLEEP_NO_RX,
    }

    internal enum FilterType : byte
    {
        CLASSIC,
        DUAL,
        RANGE,
    }
    #endregion

    internal class BinCanInterface : BinBase
    {
        internal enum BinProp
        {
            Type,
            PhysicalNumber,
            CANBusSpeed,
            CANFDBusSpeed,
            CANFDNonISO,
            TSeg1,
            TSeg2,
            SJW,
            TSeg1FD,
            TSeg2FD,
            SJWFD,
            SilentMode,
            Autodetect,
            WakeUpOption,
            SleepOption,
            SleepNoActivityTime,
            Prescaler,
            PrescalerFD,
            UseBitTiming,
            FilterCount,
            FilterID1,
            FilterID2,
            FilterAccept,
            FilterType,
            FilterExtended,
        }

        #region Do not touch these
        public BinCanInterface(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override void SetupVersions()
        {
            Versions[2] = new Action(() =>
            {
                data.AddProperty(BinProp.Type, typeof(CanType));
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.CANBusSpeed, typeof(UInt32));
                data.AddProperty(BinProp.CANFDBusSpeed, typeof(UInt32));
                data.AddProperty(BinProp.CANFDNonISO, typeof(bool)); // bool
                data.AddProperty(BinProp.TSeg1, typeof(byte));
                data.AddProperty(BinProp.TSeg2, typeof(byte));
                data.AddProperty(BinProp.SJW, typeof(byte));
                data.AddProperty(BinProp.TSeg1FD, typeof(byte));
                data.AddProperty(BinProp.TSeg2FD, typeof(byte));
                data.AddProperty(BinProp.SJWFD, typeof(byte));
                data.AddProperty(BinProp.SilentMode, typeof(bool)); // bool
                data.AddProperty(BinProp.Autodetect, typeof(bool)); // bool
            });
            Versions[3] = new Action(() =>
            {
                Versions[2].DynamicInvoke();
                data.AddProperty(BinProp.WakeUpOption, typeof(WakeUpMode));
                data.AddProperty(BinProp.SleepOption, typeof(SleepMode));
                data.AddProperty(BinProp.SleepNoActivityTime, typeof(UInt16));
            });
            Versions[4] = new Action(() =>
            {
                Versions[3].DynamicInvoke();
                data.AddProperty(BinProp.Prescaler, typeof(byte));
                data.AddProperty(BinProp.PrescalerFD, typeof(byte));
                data.AddProperty(BinProp.UseBitTiming, typeof(bool)); // bool
            });
            Versions[5] = new Action(() =>
            {
                Versions[4].DynamicInvoke();
                data.AddProperty(BinProp.FilterCount, typeof(byte));
                data.AddProperty(BinProp.FilterID1, typeof(UInt32[]), BinProp.FilterCount);
                data.AddProperty(BinProp.FilterID2, typeof(UInt32[]), BinProp.FilterCount);
                data.AddProperty(BinProp.FilterAccept, typeof(bool[]), BinProp.FilterCount); // bool
                data.AddProperty(BinProp.FilterType, typeof(FilterType[]), BinProp.FilterCount);
                //data.AddProperty(BinProp.FilterExtended, typeof(bool)); // bool
            });
            AddInput("");
            AddOutput("");
        }
    }
}