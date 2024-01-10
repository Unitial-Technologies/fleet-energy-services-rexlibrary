using InfluxShared.Generic;
using RXD.Blocks;
using RXD.DataRecords;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RXD.Base
{
    public enum ReadLogic
    {
        ReadData,
        UpdateLowestTimestamp,
        OffsetTimestamps,
        ReadPreBuffers,
    }

    public delegate bool AllowDebugInfo();

    internal class RXDataReader : IDisposable
    {
        public static bool CreateDebugFiles = false;
        public static AllowDebugInfo ExternalDebugChecker = null;

        internal static readonly UInt16 SectorSize = 0x200;
        static readonly UInt16 MaxBufferBlocks = 0x7F;
        static readonly UInt16 MaxBufferSize = (UInt16)(MaxBufferBlocks * SectorSize);
        readonly BinRXD collection;
        readonly ReadLogic logic;

        private readonly Stream rxStream;
        private readonly PinObj rxBlock;
        private bool disposedValue;

        private delegate void ParseBufferLogic(IntPtr source);
        ParseBufferLogic ParseBuffer = null;

        public UInt64 SectorsParsed = 0;
        public RecordCollection Messages = null;
        public MultiFrameCollection MultiFrames = new MultiFrameCollection();
        internal Int64 TimeOffset;

        internal protected UInt32 DataSectorStart = 0;
        protected UInt64 SectorID = 0;
        protected List<(UInt32, UInt32)> SectorMap = null;
        protected int SectorMapID = -1;

        private delegate int ReadSectorLogic(byte[] array, int offset, int count);
        private ReadSectorLogic ReadSector = null;

        public RXDataReader(BinRXD bcollection, ReadLogic logic = ReadLogic.ReadData)
        {
            this.logic = logic;
            collection = bcollection;

            ParseBuffer = GetParseLogic();
            rxStream = bcollection.GetRWStream;
            rxBlock = new PinObj(new byte[MaxBufferSize]);
            rxStream.Seek((Int64)collection.DataOffset, SeekOrigin.Begin);
            SectorID = DataSectorStart = (UInt32)(collection.DataOffset / SectorSize);

            CreateDebugFiles = collection.dataSource == DataOrigin.File && ExternalDebugChecker is not null && ExternalDebugChecker();

            OutputDebugSectors();
            ReadSector = rxStream.Read;

            CheckForPreBuffers();
        }

        #region Destructors
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    rxStream.Dispose();
                    rxBlock.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RXDataReader()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        void OutputDebugSectors()
        {
            if (!CreateDebugFiles)
                return;

            UInt64 sid = DataSectorStart;
            byte[] buffer = new byte[SectorSize];
            RecPreBuffer.DataRecord LastPB = new RecPreBuffer.DataRecord();
            UInt32[] PBTimestamps = new UInt32[6];
            UInt32 LastTimestamp = 0;
            UInt32 pboffset = 0;
            byte[] tmp;
            using (FileStream dbg = new FileStream(Path.ChangeExtension(collection.rxdUri, ".sectors"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (FileStream fs = new FileStream(collection.rxdUri, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(DataSectorStart * SectorSize, SeekOrigin.Begin);
                while (fs.Read(buffer, 0, SectorSize) > 0)
                {
                    string msg;
                    if (collection.TryGetValue(BitConverter.ToUInt16(buffer, 2), out BinBase bb) && bb.RecType == RecordType.PreBuffer)
                    {
                        bool isFirst = pboffset == 0;

                        if (isFirst)
                            pboffset = BitConverter.ToUInt32(buffer, 10) - 1;
                        else
                        {
                            msg = $"Previous PreBuffer info - PreBuffer diff: {PBTimestamps[3] - PBTimestamps[2]}; PostBuffer diff: {PBTimestamps[5] - PBTimestamps[4]}";
                            tmp = Encoding.ASCII.GetBytes(msg + Environment.NewLine);
                            dbg.Write(tmp, 0, tmp.Length);
                        }

                        LastPB.Timestamp = BitConverter.ToUInt32(buffer, 6);
                        LastPB.PreStartSector = BitConverter.ToUInt32(buffer, 10) - pboffset + DataSectorStart;
                        LastPB.PreCurrentSector = BitConverter.ToUInt32(buffer, 14) - pboffset + DataSectorStart;
                        LastPB.PreEndSector = BitConverter.ToUInt32(buffer, 18) - pboffset + DataSectorStart;
                        LastPB.PostStartSector = BitConverter.ToUInt32(buffer, 22) - pboffset + DataSectorStart;
                        LastPB.PostEndSector = BitConverter.ToUInt32(buffer, 26) - pboffset + DataSectorStart;
                        LastPB.NextPreBufferSector = BitConverter.ToUInt32(buffer, 30) - pboffset + DataSectorStart;

                        msg = $"Sector {sid:D6} (0x{sid:X4}) - PreBuffer info; " +
                            $"Timestamp: {LastPB.Timestamp}; " +
                            $"PreStart: {LastPB.PreStartSector}; " +
                            $"PreCurr: {LastPB.PreCurrentSector}; " +
                            $"PreEnd: {LastPB.PreEndSector}; " +
                            $"PostStart: {LastPB.PostStartSector}; " +
                            $"PostEnd: {LastPB.PostEndSector}; " +
                            $"NextPB: {LastPB.NextPreBufferSector}; "
                            ;
                    }
                    else
                    {
                        UInt32 ts = BitConverter.ToUInt32(buffer, 6);
                        string lh = ts < LastTimestamp ? "Lowest" : "Highest";
                        msg = $"Sector {sid:D6} (0x{sid:X4}) - Timestamp: {ts}; {lh}";
                        LastTimestamp = ts;

                        if (sid == LastPB.PreStartSector)
                            PBTimestamps[0] = ts;
                        else if (sid == LastPB.PreCurrentSector)
                            PBTimestamps[1] = ts;
                        else if (sid == LastPB.PreCurrentSector + 1)
                            PBTimestamps[2] = ts;
                        else if (sid == LastPB.PreEndSector)
                            PBTimestamps[3] = ts;
                        else if (sid == LastPB.PostStartSector)
                            PBTimestamps[4] = ts;
                        else if (sid == LastPB.PostEndSector)
                            PBTimestamps[5] = ts;
                    }
                    tmp = Encoding.ASCII.GetBytes(msg + Environment.NewLine);
                    dbg.Write(tmp, 0, tmp.Length);

                    sid++;
                }
            }
        }

        ParseBufferLogic GetParseLogic()
        {
            switch (logic)
            {
                case ReadLogic.ReadData: return ParseBufferData;
                case ReadLogic.UpdateLowestTimestamp: return ParseBufferTimestamps;
                case ReadLogic.OffsetTimestamps: return OffsetTimestamps;
                case ReadLogic.ReadPreBuffers: return ParsePreBuffers;
                default: return null;
            }
        }

        void GetBlockBounds(ref IntPtr source, out IntPtr eobPtr)
        {
            UInt16 blocksize = (UInt16)Marshal.ReadInt16(source);
            source += Marshal.SizeOf(blocksize);
            eobPtr = source + blocksize;
        }

        void ParseBufferData(IntPtr source)
        {
            StringBuilder sb = null;
            StringBuilder dbg = null;
            if (CreateDebugFiles)
            {
                sb = new StringBuilder();
                sb.AppendLine("New block");
                dbg = new StringBuilder();
            }

            GetBlockBounds(ref source, out IntPtr endptr);
            while ((long)source < (long)endptr)
            {
                RecRaw rec = RecRaw.Read(ref source);

                if (CreateDebugFiles)
                {
                    if (rec.header.UID == 0xFFFF)
                        dbg.Append(Encoding.ASCII.GetString(rec.VariableData));
                    else
                        sb.Append(rec.ToString());
                }

                if (rec.header.InfSize < 4)
                    continue;

                if (!collection.TryGetValue(rec.header.UID, out rec.LinkedBin))
                    continue;

                if (rec.LinkedBin.RecType == RecordType.Unknown)
                    continue;

                rec.BusChannel = collection.DetectBusChannel(rec.header.UID);
                RecBase input = RecBase.Parse(rec);

                if (input is null)
                    break;

                Messages.Add(input);
            }

            CheckForMultiFrame();

            if (CreateDebugFiles)
            {
                if (sb.Length > 0)
                    using (var reclog = File.AppendText(Path.ChangeExtension(collection.rxdUri, ".records")))
                        reclog.Write(sb.ToString());
                if (dbg.Length > 0)
                    using (var reclog = File.AppendText(Path.ChangeExtension(collection.rxdUri, ".aws")))
                        reclog.Write(dbg.ToString());
            }
        }

        void ParseBufferTimestamps(IntPtr source)
        {
            GetBlockBounds(ref source, out IntPtr endptr);
            while ((long)source < (long)endptr)
            {
                RecRaw rec = RecRaw.Read(ref source);
                if (collection.TryGetValue(rec.header.UID, out BinBase bin) && !bin.DataFound)
                {
                    bin.LowestTimestamp = BitConverter.ToUInt32(rec.Data, 0);
                    //collection[rec.header.UID].HighestTimestamp = tmpTime;
                    bin.DataFound = true;
                }
            }
        }

        void OffsetTimestamps(IntPtr source)
        {
            if (TimeOffset == 0)
                return;

            GetBlockBounds(ref source, out IntPtr endptr);
            while ((long)source < (long)endptr)
                RecRaw.ApplyTimestampOffset(ref source, TimeOffset);
        }

        public void ReadLiveBuffer(PinObj buffer, int BlockCount)
        {
            Messages = new RecordCollection();
            for (int i = 0; i < BlockCount; i++)
                ParseBufferData((IntPtr)buffer + i * 512);
        }

        void ParsePreBuffers(IntPtr source)
        {
            GetBlockBounds(ref source, out IntPtr endptr);
            while ((long)source < (long)endptr)
            {
                RecRaw rec = RecRaw.Read(ref source);

                if (!collection.TryGetValue(rec.header.UID, out rec.LinkedBin))
                    continue;

                if (rec.LinkedBin.RecType != RecordType.PreBuffer)
                    continue;

                //rec.BusChannel = collection.DetectBusChannel(rec.header.UID);
                RecBase input = RecBase.Parse(rec);

                if (input is null)
                    break;

                Messages.Add(input);
            }
        }

        int ReadPreBufferSector(byte[] array, int offset, int count)
        {
            //if (SectorMap[SectorMapID].Item2 == 0)
            //return 0;
            if (SectorID == SectorMap[SectorMapID].Item1)
            {
                rxStream.Seek(SectorMap[SectorMapID].Item2 * SectorSize, SeekOrigin.Begin);
                SectorMapID++;
            }
            SectorID++;
            return rxStream.Read(array, offset, count);
        }

        public bool ReadNext()
        {
            Messages = null;
            try
            {
                // Data block
                //while (fsRXD.FastRead<byte>(rxBlock, 0, 512) > 0)
                if (ReadSector(rxBlock, 0, SectorSize) != SectorSize)
                    return false;

                UInt16 blocksize = (UInt16)Marshal.ReadInt16(rxBlock);
                int blocks = ((2 + blocksize + 0x1ff) & ~(UInt16)0x1ff) / SectorSize;
                if (blocks > 1)
                    ReadSector(rxBlock, (blocks - 1) * SectorSize, SectorSize);

                Messages = new RecordCollection();
                Messages.ID = ++SectorsParsed;
                ParseBuffer?.Invoke(rxBlock);
                if (logic == ReadLogic.OffsetTimestamps && TimeOffset != 0)
                {
                    rxStream.Seek(-SectorSize, SeekOrigin.Current);
                    rxStream.Write(rxBlock, 0, SectorSize);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public UInt32 ReadSectorHighestTimestamp(Int64 fileposTimeOffset)
        {
            try
            {
                rxStream.Seek(fileposTimeOffset, SeekOrigin.Begin);
                if (rxStream.Read(rxBlock, 0, SectorSize) != SectorSize)
                    return 0;

                UInt16 blocksize = (UInt16)Marshal.ReadInt16(rxBlock);
                int blocks = ((2 + blocksize + 0x1ff) & ~(UInt16)0x1ff) / SectorSize;
                if (blocks > 1)
                    ReadSector(rxBlock, SectorSize, (blocks - 1) * SectorSize);

                UInt32 LastTime = 0;
                IntPtr source = rxBlock;
                GetBlockBounds(ref source, out IntPtr endptr);
                while ((long)source < (long)endptr)
                {
                    RecRaw rec = RecRaw.Read(ref source);

                    if (!collection.TryGetValue(rec.header.UID, out rec.LinkedBin))
                        continue;

                    LastTime = Math.Max(LastTime, BitConverter.ToUInt32(rec.Data, 0));
                }

                return LastTime;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public Int64 GetProgress => (rxStream.Position * 100) / collection.rxdSize;

        void CheckForMultiFrame()
        {
            for (int i = 0; i < Messages.Count; i++)
            {
                RecBase record = Messages[i];
                if (record.LinkedBin != null)
                    if (record.LinkedBin.BinType == BlockType.CANMessage && record.LinkedBin.RecType == RecordType.CanTrace)
                    {
                        RecCanTrace msg = record as RecCanTrace;

                        //if (msgBlock[BinCanMessage.BinProp.isJ1939] == true)
                        if ((msg.data.Flags & J1939.pgnFlagsMask) == J1939.pgnFlagsValue)
                        {
                            if (J1939.isPgnConnectMessage(msg))
                            {
                                MultiFrameData data = MultiFrames.AddOrGetJ1939(msg);
                                if (data.Count > 0)
                                {
                                    //MessageBox.Show("Previous buffer untriggered");
                                }
                                data.Clear();
                                data.Add(msg);
                            }
                            else if (J1939.isPgnDataTransferMessage(msg))
                            {
                                MultiFrameData data = MultiFrames.GetJ1939(msg.data.CanID);
                                if (data != null)
                                {
                                    data.Add(msg);

                                    if (data.isCompleted)
                                    {
                                        Messages.Insert(i + 1, data.PackJ1939Message());
                                        data.Clear();
                                    }
                                }
                            }
                        }
                    }
            }
        }

        bool isActive()
        {
            try
            {
                byte[] tmp = new byte[SectorSize];
                rxStream.Seek(-SectorSize, SeekOrigin.End);
                rxStream.Read(tmp, 0, SectorSize);
                return tmp.All(b => b == 0);
            }
            catch
            {
                return false;
            }
        }

        internal UInt32 GetFilePreBufferInitialTimestamp
        {
            get
            {
                if (collection.PreBuffers.Count > 0)
                    if (collection.PreBuffers[0].data.PreStartSector == DataSectorStart)
                        return collection.PreBuffers[0].data.InitialTimestamp;

                return 0;
            }
        }

        void CheckForPreBuffers()
        {
            if (collection.PreBuffers is null)
            {
                collection.PreBuffers = new PreBufferCollection();

                var oldLogic = ParseBuffer;
                try
                {
                    ParseBuffer = ParsePreBuffers;

                    UInt32 SectorOffset = 0;
                    while (ReadNext())
                    {
                        // Probably an error
                        if (Messages.Count == 0)
                            continue;

                        RecPreBuffer pb = Messages[0] as RecPreBuffer;
                        if (SectorOffset == 0)
                            SectorOffset = (UInt32)(pb.data.PreStartSector - (rxStream.Position / SectorSize));

                        pb.FixOffsetBy(SectorOffset);
                        collection.PreBuffers.Add(pb);
                        if (pb.data.isLast)
                            break;

                        rxStream.Seek(pb.data.NextPreBufferSector * SectorSize, SeekOrigin.Begin);
                    }
                }
                catch { }
                finally
                {
                    collection.PreBuffers.IncludeLastUntriggered = !isActive();
                    ParseBuffer = oldLogic;
                    rxStream.Seek((Int64)collection.DataOffset, SeekOrigin.Begin);
                }
            }

            if (collection.PreBuffers is not null)
            {
                SectorMap = collection.PreBuffers.GetSectorMap();
                if (SectorMap.Count > 0)
                {
                    SectorMap.Add((0, 0));
                    SectorMapID = 0;
                    ReadSector = ReadPreBufferSector;
                }
            }
        }

    }
}
