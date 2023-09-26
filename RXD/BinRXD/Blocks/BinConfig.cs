using System;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    
    public enum Encrypt_Type : byte
    {
        No_Encryption,
        RSA
    }
    enum ConfigSleepMode : byte
    {
        NO_SLEEP,
        DEEP_SLEEP,
        NORMAL_SLEEP
    }

    internal enum WAKEUP_SOURCE : byte
    {
        ALARM_SCHEDULE,
        ALARM_ONCE,
        ALARM_AFTER_SLEEP,
        MOVEMENT,
        CAN0,
        CAN1,
    }
    #endregion

    public class BinConfig : BinBase
    {
        internal enum BinProp
        {
            GUID,
            InterfaceCount,
            NameSize,
            Name,
            TimeStampSize,
            TimeStampPrecision,
            SleepMode,
            ConfigurationVersion,
            CANPositiveTimeout,
            CANReceiveTimeout,
            CANSilentDelay,
            WakeUpSourceListSize,
            WakeupSourceType,
            WakeupParameter,
            EncryptType,
            EncryptKeySize,
            EncryptKey,
            LockDevice,
            EncryptDataLog,
            NTPServerSize,
            NTPServer
        }

        #region Do not touch these
        public BinConfig(BinHeader hs = null) : base(hs)
        {
            header.uniqueid = 0;
        }

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
                data.AddProperty(BinProp.GUID, typeof(Guid));
                data.AddProperty(BinProp.InterfaceCount, typeof(UInt16));
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
                data.AddProperty(BinProp.TimeStampSize, typeof(byte));
                data.AddProperty(BinProp.TimeStampPrecision, typeof(UInt32));
            });
            Versions[3] = new Action(() =>
            {
                Versions[2].DynamicInvoke();
                data.AddProperty(BinProp.SleepMode, typeof(ConfigSleepMode));
            });
            Versions[4] = new Action(() =>
            {
                Versions[3].DynamicInvoke();
                data.AddProperty(BinProp.ConfigurationVersion, typeof(UInt16));
            });
            Versions[5] = new Action(() =>
            {
                Versions[4].DynamicInvoke();
                data.AddProperty(BinProp.CANPositiveTimeout, typeof(UInt16));
                data.AddProperty(BinProp.CANSilentDelay, typeof(UInt16));
                data.AddProperty(BinProp.CANReceiveTimeout, typeof(UInt16));
                data.AddProperty(BinProp.WakeUpSourceListSize, typeof(byte));
                data.AddProperty(BinProp.WakeupSourceType, typeof(WAKEUP_SOURCE[]), BinProp.WakeUpSourceListSize);
            });
            Versions[6] = new Action(() =>
            {
                Versions[5].DynamicInvoke();
                data.AddProperty(BinProp.WakeupParameter, typeof(UInt32[]), BinProp.WakeUpSourceListSize);
                data.Property(BinProp.WakeupSourceType).XmlSequenceGroup = "WAKEUP_SOURCE";
                data.Property(BinProp.WakeupParameter).XmlSequenceGroup = "WAKEUP_SOURCE";
            });
            Versions[7] = new Action(() =>
            {
                Versions[6].DynamicInvoke();
                data.AddProperty(BinProp.EncryptType, typeof(Encrypt_Type));
                data.AddProperty(BinProp.EncryptKeySize, typeof(UInt16));
                data.AddProperty(BinProp.EncryptKey, typeof(string), BinProp.EncryptKeySize);
                data.AddProperty(BinProp.LockDevice, typeof(bool));
                data.AddProperty(BinProp.EncryptDataLog, typeof(bool));
            });
            Versions[8] = new Action(() =>
            {
                Versions[7].DynamicInvoke();
                data.AddProperty(BinProp.NTPServerSize, typeof(byte));
                data.AddProperty(BinProp.NTPServer, typeof(string), BinProp.NTPServerSize);
            });
        }
    }
}