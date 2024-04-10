using InfluxShared.FileObjects;
using RXD.Blocks;
using RXD.DataRecords;
using SharedObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RXD.Base
{
    public class BlockCollection : Dictionary<Int64, BinBase>
    {
        internal BinConfig Config;
        internal BinConfigFTP ConfigFTP;
        internal BinConfigMobile ConfigMobile;
        internal BinConfigS3 ConfigS3;
        internal PreBufferCollection PreBuffers = null;
        internal double TimestampCoeff;
        internal UInt32 LowestTimestamp;
        internal bool LowestTimestampDetected = false;
        private UInt16 FUID = 0;  // Auto Increment ID
        public UInt16 NewUID { get { return ++FUID; } }

        public BlockCollection()
        {
            Config = new BinConfig();
        }

        public void Add(BinBase block)
        {
            if (block == null)
                return;

            if (block.BinType == BlockType.Config_Ftp)
                ConfigFTP = (BinConfigFTP)block;
            else if (block.BinType == BlockType.Config_Mobile)
                ConfigMobile = (BinConfigMobile)block;
            else if (block.BinType == BlockType.CONFIG_S3)
                ConfigS3 = (BinConfigS3)block;
            else
                Add(block.header.uniqueid, block);
        }

        internal void DetectLowestTimestamp()
        {
            if (LowestTimestampDetected)
                return;

            LowestTimestampDetected = true;
            using (RXDataReader dr = new RXDataReader(this as BinRXD, ReadLogic.UpdateLowestTimestamp))
                while (dr.ReadNext()) ;

            bool firsttime = true;
            foreach (var bin in this)
                if (bin.Value.DataFound)
                {
                    if (firsttime)
                    {
                        LowestTimestamp = bin.Value.LowestTimestamp;
                        //HighestTimestamp = bin.Value.HighestTimestamp;
                        firsttime = false;
                    }
                    else if (bin.Value.LowestTimestamp < LowestTimestamp)
                        LowestTimestamp = bin.Value.LowestTimestamp;
                    /*else if (bin.Value.LowestTimestamp > HighestTimestamp)
                        HighestTimestamp = bin.Value.HighestTimestamp;*/
                }
        }

        public void OffsetTimestamps(Int64 TimeOffset)
        {
            TimeOffset = (Int64)(TimeOffset * (double)(1000 / Config[BinConfig.BinProp.TimeStampPrecision]));

            using (RXDataReader dr = new RXDataReader(this as BinRXD, ReadLogic.UpdateLowestTimestamp))
                while (dr.ReadNext()) ;

            using (RXDataReader dr = new RXDataReader(this as BinRXD, ReadLogic.OffsetTimestamps))
            {
                dr.TimeOffset = TimeOffset - LowestTimestamp;
                while (dr.ReadNext()) ;
            }

            LowestTimestamp = (UInt32)TimeOffset;
        }

        public UInt32 GetHighestTimestampInBlockAt(Int64 fileposTimeOffset)
        {
            using (RXDataReader dr = new RXDataReader(this as BinRXD))
                return dr.ReadSectorHighestTimestamp(fileposTimeOffset);
        }

        public byte DetectBusChannel(UInt16 uid)
        {
            BinBase bin = this[uid];
            switch (bin.BinType)
            {
                case BlockType.CANInterface: return (bin as BinCanInterface)[BinCanInterface.BinProp.PhysicalNumber];
                case BlockType.CANMessage: return DetectBusChannel(((BinCanMessage)bin)[BinCanMessage.BinProp.InterfaceUID]);
                case BlockType.SDMessage: return DetectBusChannel(((BinSDMessage)bin)[BinSDMessage.BinProp.InterfaceUID]);
                case BlockType.CANError: return DetectBusChannel(((BinCanError)bin)[BinCanError.BinProp.InterfaceID]);
                case BlockType.LINMessage: return DetectBusChannel(((BinLinMessage)bin)[BinLinMessage.BinProp.InterfaceID]);
                default:
                    return 0;
            }
        }

        public byte GetLastBusID => this.Values.OfType<BinCanInterface>().Max(x => x[BinCanInterface.BinProp.PhysicalNumber]);

        internal bool DbcEqualsRecord(ExportDbcMessage busMsg, RecCanTrace record)
        {
            if (busMsg.BusChannel == record.BusChannel)
                switch (busMsg.Message.MsgType)
                {
                    case DBCMessageType.Standard:
                        if (!record.data.Flags.HasFlag(MessageFlags.EDL) && !record.data.Flags.HasFlag(MessageFlags.IDE))
                            return busMsg.Message.CANID == record.data.CanID;
                        else
                            return false;
                    case DBCMessageType.Extended:
                        if (!record.data.Flags.HasFlag(MessageFlags.EDL) && record.data.Flags.HasFlag(MessageFlags.IDE))
                            return busMsg.Message.CANID == record.data.CanID;
                        else
                            return false;
                    case DBCMessageType.CanFDStandard:
                        if (record.data.Flags.HasFlag(MessageFlags.EDL) && !record.data.Flags.HasFlag(MessageFlags.IDE))
                            return busMsg.Message.CANID == record.data.CanID;
                        else
                            return false;
                    case DBCMessageType.CanFDExtended:
                        if (record.data.Flags.HasFlag(MessageFlags.EDL) && record.data.Flags.HasFlag(MessageFlags.IDE))
                            return busMsg.Message.CANID == record.data.CanID;
                        else
                            return false;
                    case DBCMessageType.J1939PG:
                        if ((record.data.Flags & J1939.pgnFlagsMask) == J1939.pgnFlagsValue)
                            return CanIdentifier.IsPassFilter(busMsg.Message.CANID, record.data.CanID);
                        else
                            return false;
                    case DBCMessageType.KanCan:
                        if (!record.data.Flags.HasFlag(MessageFlags.EDL) && record.data.Flags.HasFlag(MessageFlags.IDE))
                            return busMsg.Message.CANID == record.data.CanID.KanCanId;
                        else
                            return false;
                    default:
                        return false;
                }
            else
                return false;
        }

        internal bool LdfEqualsRecord(ExportLdfMessage busMsg, RecLinTrace record) => busMsg.BusChannel == record.BusChannel && busMsg.Message.ID == record.data.LinID;

        public void BuildTCStruct(List<string> TCNames)
        {
            Config[BinConfig.BinProp.GUID] = Guid.NewGuid();
            Config[BinConfig.BinProp.TimeStampSize] = 4;
            Config[BinConfig.BinProp.TimeStampPrecision] = 1000;

            for (UInt16 i = 1; i <= 32; i++)
            {
                BinCanSignal sig = new BinCanSignal();
                sig.header.uniqueid = i;
                if (TCNames is null)
                    sig[BinCanSignal.BinProp.Name] = "TC" + (i - 1).ToString();
                else
                    sig[BinCanSignal.BinProp.Name] = TCNames[i - 1];

                sig[BinCanSignal.BinProp.ParA] = 0.0625;
                sig[BinCanSignal.BinProp.ParB] = 0;
                sig[BinCanSignal.BinProp.StartBit] = 0;
                sig[BinCanSignal.BinProp.BitCount] = 16;
                sig[BinCanSignal.BinProp.Endian] = SignalByteOrder.INTEL;
                sig[BinCanSignal.BinProp.SignalType] = SignalDataType.SIGNED;
                Add(i, sig);
            }
        }

        internal BinCanMessage AddCANMessage()
        {
            BinCanMessage binMessage = new BinCanMessage();
            binMessage.header.uniqueid = NewUID;
            binMessage[BinCanMessage.BinProp.MessageIdentStart] = 0;
            binMessage[BinCanMessage.BinProp.MessageIdentEnd] = 0;
            binMessage[BinCanMessage.BinProp.Direction] = RXD.Blocks.DirectionType.Input;
            binMessage[BinCanMessage.BinProp.DLC] = 8;
            binMessage[BinCanMessage.BinProp.IsExtended] = 0;
            //            binMessage[BinCanMessage.BinProp.InterfaceUID] = binCanBus.header.uniqueid;
            binMessage[BinCanMessage.BinProp.Period] = 0;
            binMessage[BinCanMessage.BinProp.TriggeringMessageUniqueID] = 0;
            binMessage[BinCanMessage.BinProp.DefaultHex] = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 }; //{ 0x0F, 0x1E, 0x2D, 0x3C, 0x4B, 0x5A, 0x69, 0x78 };
            binMessage[BinCanMessage.BinProp.CustomAlgorithm] = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            binMessage[BinCanMessage.BinProp.CustomBytePosition] = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            binMessage[BinCanMessage.BinProp.CustomDataSize] = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            //            binMessage[BinCanMessage.BinProp.CANFD_Option] = (canBus.IsCanFD) ? RXD.Blocks.MessageType.CanFD : RXD.Blocks.MessageType.Can;
            binMessage[BinCanMessage.BinProp.InputMessageUID] = 0;
            Add(binMessage);
            return binMessage;
        }

        internal int CanMessageListLevel(BinCanMessage bin)
        {
            if (bin[BinCanMessage.BinProp.NextOutputMessageID] == 0)
                return 0;
            else
                return CanMessageListLevel(this[bin[BinCanMessage.BinProp.NextOutputMessageID]]) + 1;
        }        
    }
}
