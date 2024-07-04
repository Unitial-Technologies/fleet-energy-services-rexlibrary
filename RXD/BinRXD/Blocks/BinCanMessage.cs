using InfluxShared.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    public enum DirectionType : byte
    {
        Input,
        OutputEvent,
        OutputPeriodic
    }

    #endregion

    public class BinCanMessage : BinBase
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
            CustomAlgorithm,
            CustomBytePosition,
            CustomDataSize,
            CANFD_Option,
            InputMessageUID,
            NameSize,
            Name,
            isJ1939,
            J1939Source,
            J1939Destination,
            Downsampling,
            Delay,
            NextOutputMessageID,
          /*  ResponseUID,
            ResponseTimeout,
            ResponseAttempts,
            ResponseObligatory*/
        }

        #region Do not touch these
        public BinCanMessage(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion



        internal override void SetupVersions()
        {
            Versions[4] = new Action(() =>
            {
                data.AddProperty(BinProp.MessageIdentStart, typeof(UInt32));
                data.AddProperty(BinProp.MessageIdentEnd, typeof(UInt32), DefaultValue: 0xFFFFFFFF);
                data.AddProperty(BinProp.Direction, typeof(DirectionType));
                data.AddProperty(BinProp.DLC, typeof(byte), DefaultValue: 8);
                data.AddProperty(BinProp.IsExtended, typeof(bool)); // bool
                data.AddProperty(BinProp.InterfaceUID, typeof(UInt16));
                data.AddProperty(BinProp.Period, typeof(UInt32));
                data.AddProperty(BinProp.TriggeringMessageUniqueID, typeof(UInt16));
                data.AddProperty(BinProp.DefaultHex, typeof(byte[]), BinProp.DLC);
                data.AddProperty(BinProp.CustomAlgorithm, typeof(byte[]), 8);
                data.AddProperty(BinProp.CustomBytePosition, typeof(byte[]), 8);
                data.AddProperty(BinProp.CustomDataSize, typeof(byte[]), 8);
                data.AddProperty(BinProp.CANFD_Option, typeof(CanFDMessageType));
                data.AddProperty(BinProp.InputMessageUID, typeof(UInt16));
            });
            Versions[5] = new Action(() =>
            {
                Versions[4].DynamicInvoke();
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
            });
            Versions[6] = new Action(() =>
            {
                Versions[5].DynamicInvoke();
                data.AddProperty(BinProp.isJ1939, typeof(bool));
                data.AddProperty(BinProp.J1939Source, typeof(byte));
                data.AddProperty(BinProp.J1939Destination, typeof(byte));
            });
            Versions[7] = new Action(() =>
            {
                Versions[6].DynamicInvoke();
                data.AddProperty(BinProp.Downsampling, typeof(UInt32));
            });
            Versions[8] = new Action(() =>
            {
                Versions[7].DynamicInvoke();
                data.AddProperty(BinProp.Delay, typeof(UInt32));
                data.AddProperty(BinProp.NextOutputMessageID, typeof(UInt16));
            });
           /* Versions[9] = new Action(() =>
            {
                Versions[8].DynamicInvoke();
                data.AddProperty(BinProp.ResponseUID, typeof(ushort));
                data.AddProperty(BinProp.ResponseTimeout, typeof(uint));
                data.AddProperty(BinProp.ResponseAttempts, typeof(ushort));
                data.AddProperty(BinProp.ResponseObligatory, typeof(byte));

            });*/
            AddInput(BinProp.InterfaceUID.ToString());
            AddInput(BinProp.InputMessageUID.ToString());
            AddOutput("UID");
            AddOutput(BinProp.NextOutputMessageID.ToString());
        }
    }
}
