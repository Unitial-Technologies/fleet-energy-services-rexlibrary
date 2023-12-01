using System;
using System.Collections.Generic;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    internal enum LogFormatType : byte
    {
        InfluxGeneric1
    }
    #endregion

    internal class BinSDInterface : BinBase
    {
        internal enum BinProp
        {
            MaxLogSize,
            MaxLogTime,
            LogFormat,
            DisableUID,
            EnableUID,
            InitialEnableState,
            IsEnableCreateNewLog,
            IsPostTimeFromEnableStart,
            NumberOfLogs,
            PostLogTime,
            PreLogTime,
            PartitionID,
        }

        #region Do not touch these
        public BinSDInterface(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.MaxLogSize, typeof(UInt32));
                data.AddProperty(BinProp.MaxLogTime, typeof(UInt32));
                data.AddProperty(BinProp.LogFormat, typeof(LogFormatType));
            });
            Versions[3] = new Action(() =>
            {
                Versions[2].DynamicInvoke();
                data.AddProperty(BinProp.PreLogTime, typeof(UInt32));
                data.AddProperty(BinProp.PostLogTime, typeof(UInt32));
                data.AddProperty(BinProp.IsPostTimeFromEnableStart, typeof(bool));
                data.AddProperty(BinProp.NumberOfLogs, typeof(UInt32));
                data.AddProperty(BinProp.IsEnableCreateNewLog, typeof(bool));
                data.AddProperty(BinProp.InitialEnableState, typeof(bool));
                data.AddProperty(BinProp.EnableUID, typeof(UInt16));
                data.AddProperty(BinProp.DisableUID, typeof(UInt16));
                AddInput(BinProp.EnableUID.ToString());
                AddInput(BinProp.DisableUID.ToString());
            });
            Versions[4] = new Action(() =>
            {
                Versions[3].DynamicInvoke();
                data.AddProperty(BinProp.PartitionID, typeof(byte));
            });
            AddInput("");
        }

    }
}
