using InfluxShared.Objects;
using RXD.Blocks;
using RXD.DataRecords;
using SharedObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RXD.Base
{
    enum ReadState { Begin, Middle, End }

    internal class RXDataSyncReader : RXDataReader
    {
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        struct SyncComparer
        {
            [FieldOffset(0)]
            public UInt64 Numb;
            [FieldOffset(0)]
            public UInt32 RTC;
            [FieldOffset(4)]
            public UInt32 Time;
        }

        internal ReadState BlockState = ReadState.Begin;

        internal RXDLoggerCollection SyncLoggers;
        internal new RecordCollection MessageCollection { get; set; } = new RecordCollection();
        public override RecordCollection Messages { get => MessageCollection; set => MessageCollection = value; }

        internal CanIdentifier CanID;
        internal byte BusID;

        List<RecordCollection> MessageCollectionBuffer = new List<RecordCollection>();
        RecCanTrace syncRecord;
        BinaryData timeReader;
        BinaryData rtcReader;
        SyncComparer SyncRecordComparer = new SyncComparer { Numb = 0 };

        UInt32 SyncLag = 0;
        UInt32 SyncLastTimeDiff = 0;

        public RXDataSyncReader(BinRXD bcollection) : base(bcollection)
        {
            SyncLoggers = bcollection.AttachedLoggers;
            MessageCollection = new RecordCollection();

            if (SyncLoggers is not null)
            {
                if (DetectSyncObjects())
                    foreach (var logger in SyncLoggers)
                        if (logger.rxd is not null)
                        {
                            logger.dr = new RXDataSyncReader(logger.rxd);
                            logger.dr.timeReader = timeReader;
                            logger.dr.rtcReader = rtcReader;
                            logger.dr.CanID = CanID;
                        }
            }
        }

        bool DetectSyncObjects()
        {
            bool isValid(UInt32 uid, BinInternalParameter.Parameter_Type paramtype)
            {
                if (uid == 0)
                    return false;

                BinBase bin = collection[uid];
                return bin is BinInternalParameter binint && binint[BinInternalParameter.BinProp.Parameter_Type] == paramtype;
            }

            BinCanSignal timesig = collection.Values.OfType<BinCanSignal>().
                Where(s => isValid(s[BinCanSignal.BinProp.InputUID], BinInternalParameter.Parameter_Type.TimeStamp)).FirstOrDefault();
            if (timesig == null)
                return false;
            timeReader = timesig.DataDescriptor.CreateBinaryData();

            BinCanSignal rtcsig = collection.Values.OfType<BinCanSignal>().
                Where(s => isValid(s[BinCanSignal.BinProp.InputUID], BinInternalParameter.Parameter_Type.RTC)).FirstOrDefault();
            if (rtcsig == null)
                return false;
            rtcReader = rtcsig.DataDescriptor.CreateBinaryData();

            if (timesig[BinCanSignal.BinProp.MessageUID] != rtcsig[BinCanSignal.BinProp.MessageUID])
                return false;

            UInt16 msgUid = timesig[BinCanSignal.BinProp.MessageUID];
            if (msgUid == 0)
                return false;

            if (collection[msgUid] is BinCanMessage canmsg)
            {
                CanID = canmsg[BinCanMessage.BinProp.MessageIdentStart];
                BusID = collection.DetectBusChannel(msgUid);
                return true;
            }
            else
                return false;
        }

        internal bool SlaveReadToSyncChannel(RecCanTrace syncRecord)
        {
            //MessageCollection = new RecordCollection();
            do
            {
                if (base.MessageCollection is not null)
                    while (base.MessageCollection.Count > 0)
                    {
                        var record = base.MessageCollection[0];
                        base.MessageCollection.RemoveAt(0);
                        if (record is RecCanTrace || record is RecCanTraceError || record is RecLinTrace)
                        {
                            MessageCollection.Add(record);

                            if (syncRecord is not null && record is RecCanTrace can && can.data.CanID == syncRecord.data.CanID)
                            {
                                ParseSyncRecord(can);
                                return true;
                            }
                        }
                    }
            }
            while (base.ReadNext());

            return false;
        }

        internal bool MasterReadToSyncChannel()
        {
            do
            {
            StartReading:
                if (base.MessageCollection is not null)
                    while (base.MessageCollection.Count > 0)
                    {
                        var record = base.MessageCollection[0];
                        MessageCollection.Add(record);
                        base.MessageCollection.RemoveAt(0);

                        if (record is RecCanTrace can && can.BusChannel == BusID && can.data.CanID == CanID)
                        {
                            if (syncRecord is not null && MessageCollection.Count == 1 && syncRecord.VariableData.SequenceEqual(can.VariableData))
                            {
                                MessageCollection.Clear();
                                goto StartReading;
                            }

                            syncRecord = can;
                            return true;
                        }
                    }
            }
            while (base.ReadNext());

            syncRecord = null;
            return false;
        }

        void TrySyncMessagesTime(RecCanTrace syncRecord, UInt32 Lag, bool UseLastDiff)
        {
            if (!UseLastDiff)
            {
                var last = MessageCollection.LastOrDefault();
                if (last is null)
                    return;

                RecCanTrace canRecord = (RecCanTrace)last;
                if (canRecord.VariableData.SequenceEqual(syncRecord.VariableData))
                {
                    //UInt32 SyncRecordTimeDiff = syncRecord.RawTimestamp - canRecord.data.Timestamp;// - (canRecord.RawTimestamp - SyncRecordComparer.Time);
                    UInt32 SyncRecordTimeDiff = SyncRecordComparer.Time - canRecord.data.Timestamp + Lag + 20;
                    if (SyncLastTimeDiff == 0)
                        SyncLastTimeDiff = SyncRecordTimeDiff;
                }
                else
                    return;
            }

            for (int i = 0; i < MessageCollection.Count; i++)
                MessageCollection[i].RawTimestamp = MessageCollection[i].RawTimestamp + SyncLastTimeDiff;
        }

        void ParseSyncRecord(RecCanTrace syncRecord)
        {
            BinaryData.HexStruct hex;
            SyncRecordComparer.Numb = 0;
            if (timeReader.ExtractHex(syncRecord.VariableData, out hex))
                SyncRecordComparer.Time = (UInt32)hex.Unsigned;
            if (rtcReader.ExtractHex(syncRecord.VariableData, out hex))
                SyncRecordComparer.RTC = (UInt32)hex.Unsigned;

            SyncLag = syncRecord.RawTimestamp - SyncRecordComparer.Time;
        }

        public override bool ReadNext()
        {
            try
            {
                for (int mb = MessageCollectionBuffer.Count - 1; mb >= 0; mb--)
                    if (MessageCollectionBuffer[mb].Count == 0)
                        MessageCollectionBuffer.RemoveAt(mb);

                while (MessageCollectionBuffer.Count < 10)
                    if (ReadNextBuff())
                        MessageCollectionBuffer.Add(MessageCollection);
                    else
                        break;


                if (MessageCollectionBuffer.Count == 0)
                    return false;

                MessageCollection = new RecordCollection();
                MessageCollection.AddRange(MessageCollectionBuffer[0]);

                UInt32 time = MessageCollection.Max(m => m.RawTimestamp);
                for (int mb = 1; mb < MessageCollectionBuffer.Count; mb++)
                {
                    for (int i = MessageCollectionBuffer[mb].Count - 1; i >= 0; i--)
                    {
                        var rec = MessageCollectionBuffer[mb][i];
                        if (rec.RawTimestamp < time)
                        {
                            MessageCollection.Add(rec);
                            MessageCollectionBuffer[mb].RemoveAt(i);
                        }
                    }
                }
                MessageCollectionBuffer.RemoveAt(0);

                MessageCollection.Sort((x, y) => x.RawTimestamp.CompareTo(y.RawTimestamp));
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        internal bool ReadNextBuff()
        {
            MessageCollection = new RecordCollection();

            try
            {
                if (!MasterReadToSyncChannel())
                    BlockState = ReadState.End;

                if (MessageCollection.Count == 0)
                {
                    BlockState = ReadState.End;
                    return false;
                }

                if (BlockState != ReadState.End)
                    ParseSyncRecord(syncRecord);

                foreach (var logger in SyncLoggers)
                {
                    RecordCollection tempCollection = null;
                    while ((logger.dr.SyncRecordComparer.Numb < SyncRecordComparer.Numb || BlockState == ReadState.End) && (syncRecord is not null))
                    {
                        if (logger.dr.SlaveReadToSyncChannel(syncRecord))
                        {
                            logger.dr.BlockState = ReadState.Middle;

                            while (logger.dr.SyncRecordComparer.Numb > SyncRecordComparer.Numb && BlockState != ReadState.End)
                                if (MasterReadToSyncChannel())
                                    ParseSyncRecord(syncRecord);
                                else
                                    BlockState = ReadState.End;
                        }
                        else
                        {
                            logger.dr.BlockState = ReadState.End;
                            if (logger.dr.MessageCollection.Count > 0)
                            {
                                logger.dr.TrySyncMessagesTime(syncRecord, SyncLag, true);
                                tempCollection = logger.dr.MessageCollection;
                                logger.dr.MessageCollection.Clear();
                            }

                            if (logger.LoadNextFile())
                            {
                                logger.dr.timeReader = timeReader;
                                logger.dr.rtcReader = rtcReader;
                                logger.dr.CanID = CanID;
                                continue;
                            }
                            else
                                break;
                        }
                    }

                    logger.dr.TrySyncMessagesTime(syncRecord, SyncLag, BlockState == ReadState.End || logger.dr.BlockState == ReadState.End);
                    if (tempCollection is not null)
                        logger.dr.MessageCollection.AddRange(tempCollection);
                }

                byte BusOffset = (byte)(collection.GetLastBusID + 1);
                foreach (var logger in SyncLoggers)
                {
                    if (BlockState != ReadState.End)
                    {
                        if (logger.dr.SyncRecordComparer.Numb > SyncRecordComparer.Numb)
                        {
                            for (int i = logger.dr.MessageCollection.Count - 1; i >= 0; i--)
                            {
                                var record = logger.dr.MessageCollection[i];
                                if (record.RawTimestamp < SyncRecordComparer.Time)
                                {
                                    record.BusChannel += BusOffset;
                                    MessageCollection.Add(record);
                                    logger.dr.MessageCollection.RemoveAt(i);
                                }
                            }
                        }
                        else if (logger.dr.SyncRecordComparer.Numb < SyncRecordComparer.Numb)
                        {

                        }
                        else
                        {
                            foreach (var record in logger.dr.MessageCollection)
                                record.BusChannel += BusOffset;
                            if (BlockState == ReadState.Begin)
                            {
                                var timebegin = MessageCollection.Min(m => m.RawTimestamp);
                                MessageCollection.AddRange(logger.dr.MessageCollection.Where(r => r.RawTimestamp >= timebegin));
                            }
                            else
                                MessageCollection.AddRange(logger.dr.MessageCollection);
                            logger.dr.MessageCollection.Clear();
                        }
                    }
                    else // End of master file
                    {
                        UInt32 LastFileTime = MessageCollection.Max(x => x.RawTimestamp);

                        for (int i = logger.dr.MessageCollection.Count - 1; i >= 0; i--)
                        {
                            var record = logger.dr.MessageCollection[i];
                            if (record.RawTimestamp < LastFileTime)
                            {
                                record.BusChannel += BusOffset;
                                MessageCollection.Add(record);
                                logger.dr.MessageCollection.RemoveAt(i);
                            }
                        }
                    }

                    BusOffset += logger.CanBusCount;
                }

                if (BlockState == ReadState.Begin)
                    BlockState = ReadState.Middle;
                MessageCollection.Sort((x, y) => x.RawTimestamp.CompareTo(y.RawTimestamp));
                return MessageCollection.Count > 0;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
    }
}
