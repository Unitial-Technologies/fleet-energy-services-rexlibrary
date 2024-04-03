using System;

namespace RXD.Blocks
{
    public class BinConfigFTP : BinBase
    {
        #region Enumerations for Property type definitions
        public enum FtpConnectionType : byte
        {
            USE_SERVERNAME,
            USE_IP,
        }

        public enum FtpMode : byte
        {
            Active,
            Passive,
        }

        public enum FtpType : byte
        {
            FTP,
            FTPS,
        }
        #endregion

        internal enum BinProp
        {
            ServerName_IP_Connection_Option,
            ServerIPSize,
            ServerIP,
            ServerNameSize,
            ServerName,
            Mode,
            Type,
            Port,
            UserSize,
            User,
            PassSize,
            Pass,
            StatusSendTime,
            ConfigurationCheckTime,
            FirmwareCheckTime,
            AutomaticFirmwareUpdate,
            EncryptPass,
            UploadResume,
            UploadAllLogsBeforeSleep,
            NameSize,
            Name
        }

        #region Do not touch these
        public BinConfigFTP(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.ServerName_IP_Connection_Option, typeof(FtpConnectionType));
                data.AddProperty(BinProp.ServerIPSize, typeof(byte));
                data.AddProperty(BinProp.ServerIP, typeof(string), BinProp.ServerIPSize);
                data.AddProperty(BinProp.ServerNameSize, typeof(byte));
                data.AddProperty(BinProp.ServerName, typeof(string), BinProp.ServerNameSize);
                data.AddProperty(BinProp.Mode, typeof(FtpMode));
                data.AddProperty(BinProp.Type, typeof(FtpType));
                data.AddProperty(BinProp.Port, typeof(UInt16));
                data.AddProperty(BinProp.UserSize, typeof(byte));
                data.AddProperty(BinProp.User, typeof(string), BinProp.UserSize);
                data.AddProperty(BinProp.PassSize, typeof(byte));
                data.AddProperty(BinProp.Pass, typeof(string), BinProp.PassSize);
                data.AddProperty(BinProp.StatusSendTime, typeof(UInt32));
                data.AddProperty(BinProp.ConfigurationCheckTime, typeof(UInt32));
                data.AddProperty(BinProp.FirmwareCheckTime, typeof(UInt32));
                data.AddProperty(BinProp.AutomaticFirmwareUpdate, typeof(bool));
            });
            Versions[2] = new Action(() =>
            {
                Versions[1].DynamicInvoke();
                data.AddProperty(BinProp.EncryptPass, typeof(bool));
            });
            Versions[3] = new Action(() =>
            {
                Versions[2].DynamicInvoke();
                data.AddProperty(BinProp.UploadResume, typeof(bool), true);
            });
            Versions[4] = new Action(() =>
            {
                Versions[3].DynamicInvoke();
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
            });
            Versions[5] = new Action(() =>
            {
                Versions[4].DynamicInvoke();
                data.AddProperty(BinProp.UploadAllLogsBeforeSleep, typeof(bool));
            });
        }
    }
}