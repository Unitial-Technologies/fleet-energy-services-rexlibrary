using System;

namespace RXD.Blocks
{
    public class BinConfigMobile : BinBase
    {
        #region Enumerations for Property type definitions
        #endregion

        public enum RealTimeSourceType : byte
        {
            No,
            Mobile,
            NTP
        }

        internal enum BinProp
        {
            UseAPN,
            APNSize,
            APN,
            UserSize,
            User,
            PassSize,
            Pass,
            PinSize,
            PIN,
            RealTimeSource,
            NoCommunicationRestartTimeOut,
          //  NameSize,
          //  Name,
        }

        #region Do not touch these
        public BinConfigMobile(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.UseAPN, typeof(bool));
                data.AddProperty(BinProp.APNSize, typeof(byte));
                data.AddProperty(BinProp.APN, typeof(string), BinProp.APNSize);
                data.AddProperty(BinProp.UserSize, typeof(byte));
                data.AddProperty(BinProp.User, typeof(string), BinProp.UserSize);
                data.AddProperty(BinProp.PassSize, typeof(byte));
                data.AddProperty(BinProp.Pass, typeof(string), BinProp.PassSize);
                data.AddProperty(BinProp.PinSize, typeof(byte));
                data.AddProperty(BinProp.PIN, typeof(string), BinProp.PinSize);
                data.AddProperty(BinProp.RealTimeSource, typeof(RealTimeSourceType));
                data.AddProperty(BinProp.NoCommunicationRestartTimeOut, typeof(UInt32));
               // data.AddProperty(BinProp.NameSize, typeof(byte));
               // data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
            });
        }
    }
}