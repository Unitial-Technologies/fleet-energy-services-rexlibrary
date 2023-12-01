using InfluxShared.FileObjects;
using InfluxShared.Generic;
using InfluxShared.Helpers;
using Ionic.Zlib;
using MDF4xx.Blocks;
using MDF4xx.Frames;
using MDF4xx.IO;
using RXD.Blocks;
using RXD.DataRecords;
using RXD.Helpers;
using RXD.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Schema;

namespace RXD.Base
{
    public enum DataOrigin : byte { File, Memory }

    public class BinRXD : BlockCollection, IDisposable
    {
        public class ExportSettings
        {
            public StorageCacheType StorageCache = StorageCacheType.Disk;
            public List<UInt16> ChannelFilter = null;
            public ExportCollections SignalsDatabase = null;
            public ProcessingRulesCollection ProcessingRules = null;
            public Action<object> ProgressCallback = null;
        }

        private const string DateTimeFormat = "yyyyMMdd_HHmmss";

        public static readonly string Extension = ".rxd";
        public static readonly string EncryptedExtension = ".rxe";
        public static readonly string BinExtension = ".rxc";
        public static readonly string Filter = "ReX data (*.rxd)|*.rxd";
        public static readonly string EncryptedFilter = "ReX encrypted data (*.rxe)|*.rxe";
        public static readonly string BinFilter = "ReX configuration (*.rxc)|*.rxc";

        public static bool UseMf4Compression = true;
        public static string EncryptionContainerName = "ReXgen";
        public static byte[] EncryptionKeysBlob = null;

        static byte headersizebytes = 4;
        internal readonly DataOrigin dataSource;
        readonly internal string rxdUri = "";
        readonly internal string rxeUri = "";
        readonly internal byte[] rxdBytes = null;
        public string Error = "";

        Int64 rxdFullSize;
        public Int64 rxdSize => rxdFullSize;
        public readonly DateTime DatalogStartTime;
        public readonly string SerialNumber;
        public string DatalogStartTimeAsString => DatalogStartTime.ToString(DateTimeFormat);

        public bool Empty => Count == 0;

        internal UInt64 DataOffset = 0;

        private bool disposedValue;

        private BinRXD(string uri = "", Stream dataStream = null, Stream xsdStream = null)
        {
            rxdFullSize = 0;
            Error = "";

            if (uri != String.Empty && new Uri(uri).IsFile)
                dataSource = DataOrigin.File;
            else if (dataStream != null)
                dataSource = DataOrigin.Memory;
            else
                return;

            // If no file data provided then create empty object
            if (uri is null || uri == string.Empty)
                return;

            // If file not exist then throw an exception
            else if (dataSource == DataOrigin.File && !File.Exists(uri))
                throw new Exception("File does not exist!");

            // If file is XML then read XML structure
            else if (Path.GetExtension(uri).Equals(XmlHandler.Extension, StringComparison.OrdinalIgnoreCase))
                switch (dataSource)
                {
                    case DataOrigin.File: ReadXMLStructure(uri); break;
                    case DataOrigin.Memory: ReadXMLStructure(dataStream, xsdStream); break;
                }

            else
            {
                // If file is encrypted
                if (Path.GetExtension(uri).Equals(EncryptedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    switch (dataSource)
                    {
                        case DataOrigin.File:
                            rxeUri = uri;
                            rxdUri = Path.Combine(PathHelper.TempPath, Path.ChangeExtension(Path.GetFileName(uri), Extension));
                            if (!RXEncryption.DecryptFile(rxeUri, rxdUri))
                                throw new Exception("Access to encrypted data is rejected!");
                            goto Processing;
                        case DataOrigin.Memory:
                            throw new Exception("Encryption stream data is not supported!");
                    }
                }

                // Make local copy if needed
                if (dataSource == DataOrigin.File && !PathHelper.hasWriteAccessToFile(uri))
                {
                    string newpath = Path.Combine(PathHelper.TempPath, Path.GetFileName(uri));
                    File.Copy(uri, newpath, true);
                    uri = newpath;
                }
                if (dataSource == DataOrigin.Memory)
                {
                    using (MemoryStream ms = new())
                    {
                        dataStream.CopyTo(ms);
                        rxdBytes = ms.ToArray();
                    }
                }
                rxdUri = uri;

            Processing:

                DatalogStartTime = FileNameToDateTime(Path.GetFileNameWithoutExtension(rxdUri));
                SerialNumber = FileNameToSerialNumber(Path.GetFileNameWithoutExtension(rxdUri));
                if (!ReadRXD())
                    throw new Exception("Error reading RXC data!");

                if (Config is null || Count == 0)
                    throw new Exception("Not a valid RXD file!");

            }
        }

        public static BinRXD Create() => new BinRXD();

        public static BinRXD Load(string path = null)
        {
            try
            {
                return new BinRXD(path);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Example usage:
        /// string uri = "https://bucket.s3.eu-central-1.amazonaws.com/datalogs/RexGen Air config 500kb_0001902_20221006_103901.rxd";
        /// FileStream fs = new FileStream(fn, FileMode.Open, FileAccess.Read);
        /// BinRXD r = BinRXD.Load(uri, fs);
        /// FileStream fw = new FileStream(outfn, FileMode.Create);
        /// DataHelper.Convert(r, null, fw, "csv");
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="dataStream"></param>
        /// <param name="xsdStream"></param>
        /// <returns></returns>
        public static BinRXD Load(string uri, Stream dataStream, Stream xsdStream = null)
        {
            try
            {
                return new BinRXD(uri, dataStream, xsdStream);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #region Destructors
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Path.GetExtension(rxdUri).Equals(EncryptedExtension, StringComparison.OrdinalIgnoreCase))
                        File.Delete(rxdUri);
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

        private DateTime FileNameToDateTime(string fn)
        {
            const string pattern = "\\d{8}_\\d{6}";
            DateTime dt;

            foreach (var dtstr in Regex.Matches(fn, pattern).Cast<Match>().Where(m => m.Success).Reverse())
                if (DateTime.TryParseExact(dtstr.Value, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    return dt;

            return DateTime.Now;
        }

        private string FileNameToSerialNumber(string fn)
        {
            const string pattern = "_\\d{7}_";

            foreach (var snstr in Regex.Matches(fn, pattern).Cast<Match>().Where(m => m.Success).Reverse())
                if (int.TryParse(snstr.Value.Substring(1, 7), out _))
                    return snstr.Value.Substring(1, 7);

            return "0";
        }

        Stream GetStream => dataSource switch
        {
            DataOrigin.File => new FileStream(rxdUri, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
            DataOrigin.Memory => new MemoryStream(rxdBytes),
            _ => null,
        };

        internal Stream GetRWStream => dataSource switch
        {
            DataOrigin.File => new FileStream(rxdUri, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
            DataOrigin.Memory => new MemoryStream(rxdBytes),
            _ => null,
        };

        public bool ToRXData(Stream rxStream)
        {
            try
            {
                GetStream.CopyTo(rxStream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReadRXC(Stream rxdStream)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(rxdStream))
                {
                    Config = (BinConfig)BinBase.ReadNext(br);
                    if (Config == null)
                        return false;
                    TimestampCoeff = Config[BinConfig.BinProp.TimeStampPrecision] * 0.000001;
                    if (Config[BinConfig.BinProp.GUID] == Guid.Empty)
                        return false;

                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        BinBase binblock = BinBase.ReadNext(br);
                        if (binblock == null)
                            break;
                        Add(binblock);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        bool ReadRXD()
        {
            try
            {
                using (var rxStream = GetStream)
                {
                    rxdFullSize = rxStream.Seek(0, SeekOrigin.End);
                    rxStream.Seek(0, SeekOrigin.Begin);

                    UInt32 hdrSize = (UInt32)rxdFullSize;
                    if (Path.GetExtension(rxdUri).Equals(Path.GetExtension(Extension), StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] hdrsize = new byte[headersizebytes];
                        rxStream.Read(hdrsize, 0, headersizebytes);
                        hdrSize = BitConverter.ToUInt32(hdrsize, 0);
                    }

                    if (!ReadRXC(rxStream))
                        return false;

                    DataOffset = (UInt64)(headersizebytes + hdrSize);
                    DataOffset = (DataOffset + 0x1ff) & ~(UInt32)0x1ff;
                }

                DetectLowestTimestamp();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ToRXD(Stream rxdStream, bool StructOnly = true)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var cfgblocks = new BinBase[] { Config, ConfigFTP, ConfigMobile, ConfigS3 };
                    foreach (var cfgbin in cfgblocks)
                        if (cfgbin is not null)
                        {
                            byte[] data = cfgbin.ToBytes();
                            ms.Write(data, 0, data.Length);
                        }

                    foreach (var bin in this)
                    {
                        //if (bin.Value.external.GetProperty("Active") != false)//PETKO Test
                        // {
                        byte[] data = bin.Value.ToBytes();
                        ms.Write(data, 0, data.Length);
                        // }
                    }

                    using (BinaryWriter bw = new BinaryWriter(rxdStream, Encoding.ASCII, true))
                    {
                        if (!StructOnly)
                            bw.Write((UInt32)ms.Length);
                        bw.Write(ms.ToArray());
                        bw.Flush();
                        if (!StructOnly)
                            while (rxdStream.Position % RXDataReader.SectorSize != 0)
                                bw.Write((byte)0);
                        bw.Flush();
                        rxdStream.Position = 0;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ToRXD(string rxdFileName, bool StructOnly = true)
        {
            try
            {
                using (FileStream fw = new FileStream(rxdFileName, FileMode.Create))
                    return ToRXD(fw, StructOnly);
            }
            catch
            {
                return false;
            }
        }

        public bool ToMF4(string outputfn, ExportCollections frameSignals = null, Action<object> ProgressCallback = null)
        {
            using (FileStream fs = new FileStream(outputfn, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                return ToMf4Stream(fs, frameSignals, ProgressCallback);
            }
        }

        public Stream ToMF4(ExportCollections frameSignals = null, Action<object> ProgressCallback = null)
        {
            MemoryStream ms = new MemoryStream();
            ToMf4Stream(ms, frameSignals, ProgressCallback);
            return ms;
        }

        bool ToMf4Stream(Stream mdfStream, ExportCollections frameSignals = null, Action<object> ProgressCallback = null)
        {
            bool FindMessageFrameID(RecBase rec, out UInt16 GroupID, out byte DLC)
            {
                if (rec is RecCanTrace)
                {
                    foreach (var fsig in frameSignals.dbcCollection)
                        if (DbcEqualsRecord(fsig, rec as RecCanTrace))
                        {
                            DLC = fsig.Message.DLC;
                            GroupID = (UInt16)fsig.uniqueid;
                            return true;
                        }
                }
                else if (rec is RecLinTrace)
                {
                    if (((rec as RecLinTrace).data.Flags & LinMessageFlags.Error) == 0)
                        foreach (var fsig in frameSignals.ldfCollection)
                            if (LdfEqualsRecord(fsig, rec as RecLinTrace))
                            {
                                DLC = fsig.Message.DLC;
                                GroupID = (UInt16)fsig.uniqueid;
                                return true;
                            }
                }

                GroupID = 0;
                DLC = 0;
                return false;
            }

            UInt64 LastTimestampCan = 0;
            UInt64 TimeOffsetCan = 0;
            UInt64 LastTimestampCanError = 0;
            UInt64 TimeOffsetCanError = 0;
            UInt64 LastTimestampLin = 0;
            UInt64 TimeOffsetLin = 0;

            ProgressCallback?.Invoke(0);
            ProgressCallback?.Invoke("Writing MF4 file...");
            try
            {
                Dictionary<UInt16, ChannelDescriptor> Signals =
                     this.Where(r => r.Value.RecType == RecordType.MessageData).
                     Select(dg => new { ID = (UInt16)dg.Key, Data = dg.Value.GetDataDescriptor }).
                     ToDictionary(dg => dg.ID, dg => dg.Data);
                MDF mdf = new MDF();
                mdf.BuildLoggerStruct(DatalogStartTime, 8/*Config[BinConfig.BinProp.TimeStampSize]*/, Config[BinConfig.BinProp.TimeStampPrecision], UseMf4Compression, Signals, frameSignals);
                mdf.WriteHeader(mdfStream);

                Dictionary<FrameType, CGBlock> mdfGroups =
                    mdf.Where(r => r.Value is CGBlock).
                    Select(g => new { (g.Value as CGBlock).data.cg_record_id, g.Value }).
                    ToDictionary(g => (FrameType)g.cg_record_id, g => g.Value as CGBlock);

                mdfStream.Seek(0, SeekOrigin.End);
                var DataBlock = UseMf4Compression ?
                    mdf.FirstOrDefault(x => x.Value is DZBlock).Value :
                    mdf.FirstOrDefault(x => x.Value is DTBlock).Value;
                byte[] zipData = null;

                using (MemoryStream memZippedStream = new MemoryStream())
                {
                    using (ZlibStream zlStream = new ZlibStream(memZippedStream, CompressionMode.Compress, CompressionLevel.Level4))
                    {
                        using (RXDataReader dr = new(this))
                        {
                            UInt32 InitialTimestamp = dr.GetFilePreBufferInitialTimestamp;
                            bool FirstTimestampRead = false;
                            UInt32 FileTimestamp = 0;

                            void WriteMdfFrame(BaseDataFrame frame, RecordType frameType)
                            {
                                if (frame == null)
                                    return;

                                if (mdfGroups.TryGetValue(frame.data.Type, out CGBlock cg))
                                    cg.data.cg_cycle_count++;

                                if (!FirstTimestampRead)
                                {
                                    FirstTimestampRead = true;
                                    FileTimestamp = (uint)(InitialTimestamp == 0 ? LowestTimestamp : Math.Min(InitialTimestamp, frame.data.Timestamp));
                                }

                                void CheckTimeOverlap(ref UInt64 LastTimestamp, ref UInt64 TimeOffset)
                                {
                                    if (frame.data.Timestamp < LastTimestamp)
                                        TimeOffset += 0x100000000;
                                    LastTimestamp = frame.data.Timestamp;
                                    frame.data.Timestamp += TimeOffset - FileTimestamp;
                                }

                                switch (frameType)
                                {
                                    case RecordType.CanTrace:
                                        CheckTimeOverlap(ref LastTimestampCan, ref TimeOffsetCan);
                                        break;
                                    case RecordType.CanError:
                                        CheckTimeOverlap(ref LastTimestampCanError, ref TimeOffsetCanError);
                                        break;
                                    case RecordType.LinTrace:
                                        CheckTimeOverlap(ref LastTimestampLin, ref TimeOffsetLin);
                                        break;
                                    default:
                                        CheckTimeOverlap(ref LastTimestampCan, ref TimeOffsetCan);
                                        break;
                                }

                                if (UseMf4Compression)
                                {
                                    byte[] data = frame.ToBytes();
                                    zlStream.Write(data, 0, data.Length);

                                    (DataBlock as DZBlock).data.dz_org_data_length += (UInt64)data.Length;
                                }
                                else
                                {
                                    byte[] data = frame.ToBytes();
                                    mdfStream.Write(data, 0, data.Length);
                                }
                            }

                            while (dr.ReadNext())
                            {
                                foreach (RecBase rec in dr.Messages)
                                {
                                    foreach (var mdfframe in rec.ToMdfFrame())
                                        WriteMdfFrame(mdfframe, rec.LinkedBin.RecType);

                                    if (frameSignals != null)
                                        if (FindMessageFrameID(rec, out UInt16 groupid, out byte DLC))
                                            WriteMdfFrame(rec.ConvertToMdfMessageFrame(groupid, DLC), rec.LinkedBin.RecType);
                                }
                                ProgressCallback?.Invoke((int)dr.GetProgress);
                            }
                        }
                    }
                    if (UseMf4Compression)
                        zipData = memZippedStream.ToArray();
                }
                if (UseMf4Compression)
                {
                    mdfStream.Write(zipData, 0, zipData.Length);
                    mdfStream.Flush();
                }
                Int64 eof = mdfStream.Position;

                using (BinaryWriter bw = new BinaryWriter(mdfStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), true))
                {
                    DataBlock.header.length = (UInt64)(mdfStream.Position - DataBlock.flink);
                    if (DataBlock is DZBlock)
                        (DataBlock as DZBlock).data.dz_data_length = (UInt64)zipData.Length;
                    bw.Seek((int)DataBlock.flink, SeekOrigin.Begin);
                    bw.Write(DataBlock.ToBytes(true));

                    foreach (var cg in mdfGroups)
                    {
                        bw.Seek((int)cg.Value.flink, SeekOrigin.Begin);
                        bw.Write(cg.Value.ToBytes());
                    }
                }


                ProgressCallback?.Invoke(100);
                //Console.WriteLine("file written successfully");
                return true;

            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                return false;
            }
        }

        public DoubleDataCollection ToDoubleData(ExportSettings settings = null)
        {
            settings ??= new();

            bool FindMessageFrameID(RecBase rec, out int Index)
            {
                if (rec is RecCanTrace)
                {
                    for (Index = 0; Index < settings.SignalsDatabase.dbcCollection.Count; Index++)
                        if (DbcEqualsRecord(settings.SignalsDatabase.dbcCollection[Index], rec as RecCanTrace))
                            return true;
                }
                else if (rec is RecLinTrace)
                {
                    if (((rec as RecLinTrace).data.Flags & LinMessageFlags.Error) == 0)
                        for (Index = 0; Index < settings.SignalsDatabase.ldfCollection.Count; Index++)
                            if (LdfEqualsRecord(settings.SignalsDatabase.ldfCollection[Index], rec as RecLinTrace))
                                return true;
                }

                Index = -1;
                return false;
            }

            bool Exportable(BinBase bin) => settings.ChannelFilter is null || settings.ChannelFilter.Contains(bin.header.uniqueid);

            DoubleDataCollection ddata = new(SerialNumber, settings.StorageCache);
            ddata.RealTime = DatalogStartTime;
            ddata.ProcessingRules = settings.ProcessingRules;

            UInt32 FileTimestamp = 0;
            UInt64 LastTimestampCan = 0;
            UInt64 LastTimestampLin = 0;
            UInt64 TimeOffsetCan = 0;
            UInt64 TimeOffsetLin = 0;

            double WriteData(DoubleData dd, UInt64 Timestamp, byte[] BinaryArray, ref UInt64 LastTimestamp, ref UInt64 TimeOffset)
            {
                if (Timestamp < LastTimestamp)
                    TimeOffset += 0x100000000;
                LastTimestamp = Timestamp;
                Timestamp += TimeOffset;

                return dd.WriteBinaryData((Timestamp - FileTimestamp) * TimestampCoeff, BinaryArray);
            }

            try
            {
                settings.ProgressCallback?.Invoke(0);
                settings.ProgressCallback?.Invoke("Extracting channel data...");
                using (RXDataReader dr = new RXDataReader(this))
                {
                    UInt32 InitialTimestamp = dr.GetFilePreBufferInitialTimestamp;
                    FileTimestamp = InitialTimestamp == 0 ? LowestTimestamp : InitialTimestamp;
                    //ddata.FirstTimestamp = InitialTimestamp == 0 ? double.NaN : (InitialTimestamp * TimestampCoeff);
                    if (settings.ProcessingRules is not null)
                        settings.ProcessingRules.FirstTime = (LowestTimestamp - FileTimestamp) * TimestampCoeff;

                    while (dr.ReadNext())
                    {
                        foreach (RecBase rec in dr.Messages)
                            switch (rec.LinkedBin.RecType)
                            {
                                case RecordType.Unknown:
                                    break;
                                case RecordType.CanTrace:
                                    RecCanTrace canrec = rec as RecCanTrace;
                                    if (settings.SignalsDatabase != null)
                                        if (FindMessageFrameID(canrec, out int id))
                                        {
                                            ExportDbcMessage busMsg = settings.SignalsDatabase.dbcCollection[id];
                                            byte SA = (byte)((busMsg.Message.MsgType == DBCMessageType.J1939PG) ? canrec.data.CanID.Source : 0xFF);

                                            var mode = busMsg.GetMode();
                                            double modeval = double.NaN;
                                            double lastval = double.NaN;
                                            for (int i = 0; i < busMsg.Signals.Count; i++)
                                            {
                                                var sig = busMsg.Signals[i];
                                                var obj = ddata.Object(sig, (1u << 30) | ((uint)i << 16) | (uint)id, SA);
                                                obj.BusChannel = $"CAN{canrec.BusChannel}";

                                                if (i == 1 && mode is not null)
                                                    modeval = lastval;
                                                if ((sig.Type == DBCSignalType.ModeDependent && sig.Mode == modeval) || sig.Type != DBCSignalType.ModeDependent)
                                                    lastval = WriteData(obj, canrec.data.Timestamp, rec.VariableData, ref LastTimestampCan, ref TimeOffsetCan);
                                            }
                                        }
                                    break;
                                case RecordType.CanError:
                                    break;
                                case RecordType.LinTrace:
                                    RecLinTrace linrec = rec as RecLinTrace;
                                    if (settings.SignalsDatabase != null)
                                        if (FindMessageFrameID(linrec, out int id))
                                        {
                                            ExportLdfMessage busMsg = settings.SignalsDatabase.ldfCollection[id];
                                            for (int i = 0; i < busMsg.Signals.Count; i++)
                                            {
                                                var obj = ddata.Object(busMsg.Signals[i], (2u << 30) | ((uint)i << 16) | (uint)id);
                                                obj.BusChannel = "LIN";
                                                WriteData(obj, linrec.data.Timestamp, rec.VariableData, ref LastTimestampLin, ref TimeOffsetLin);
                                            }
                                        }
                                    break;
                                case RecordType.MessageData:
                                    if (Exportable(rec.LinkedBin))
                                    {
                                        ddata.Object(rec.LinkedBin).BusChannel = $"CAN{rec.BusChannel}";
                                        WriteData(ddata.Object(rec.LinkedBin), (rec as RecMessage).data.Timestamp, rec.VariableData, ref LastTimestampCan, ref TimeOffsetCan);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        settings.ProgressCallback?.Invoke((int)dr.GetProgress);
                    }
                    if (settings.ProcessingRules is not null)
                        ddata.FinishWrite((Math.Max(LastTimestampCan, LastTimestampLin) - FileTimestamp) * TimestampCoeff);
                }

                ddata.SortByIdentifier();
                settings.ProgressCallback?.Invoke(100);
                return ddata;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal void ProcessTraceRecords(Action<TraceCollection> ProcessCallback, Action<object> ProgressCallback = null)
        {
            if (ProcessCallback is null)
                return;
            if (Count == 0)
                return;

            UInt32 TimePrecison = Config[BinConfig.BinProp.TimeStampPrecision];

            double FileTimestamp = double.NaN;
            double LastTimestampCan = 0;
            double TimeOffsetCan = 0;
            double LastTimestampLin = 0;
            double TimeOffsetLin = 0;
            double LastTimestampCanError = 0;
            double TimeOffsetCanError = 0;
            UInt32 InitialTimestamp = 0;

            void TraceAdd(TraceCollection tc, ref double LastTimestamp, ref double TimeOffset)
            {
                for (int i = 0; i < tc.Count; i++)
                {
                    if (Double.IsNaN(FileTimestamp))
                        FileTimestamp = (InitialTimestamp == 0 ? LowestTimestamp : InitialTimestamp) * TimestampCoeff;
                    //FileTimestamp = (InitialTimestamp == 0 ? LowestTimestamp : Math.Min(LowestTimestamp, InitialTimestamp)) * TimestampCoeff;

                    if (tc[i]._Timestamp < LastTimestamp)
                        TimeOffset += (double)0x100000000 * TimePrecison * 0.000001;
                    LastTimestamp = tc[i]._Timestamp;
                    tc[i]._Timestamp -= FileTimestamp;
                    tc[i]._Timestamp += TimeOffset;
                }

                ProcessCallback(tc);
            }

            ProgressCallback?.Invoke(0);
            ProgressCallback?.Invoke("Processing trace data...");
            using (RXDataReader dr = new RXDataReader(this))
            {
                InitialTimestamp = dr.GetFilePreBufferInitialTimestamp;
                while (dr.ReadNext())
                {
                    foreach (RecBase rec in dr.Messages)
                        switch (rec.LinkedBin.RecType)
                        {
                            case RecordType.Unknown:
                                break;
                            case RecordType.CanTrace:
                                TraceAdd(rec.ToTraceRow(TimePrecison), ref LastTimestampCan, ref TimeOffsetCan);
                                break;
                            case RecordType.CanError:
                                TraceAdd(rec.ToTraceRow(TimePrecison), ref LastTimestampCanError, ref TimeOffsetCanError);
                                break;
                            case RecordType.LinTrace:
                                TraceAdd(rec.ToTraceRow(TimePrecison), ref LastTimestampLin, ref TimeOffsetLin);
                                break;
                            case RecordType.PreBuffer:
                                ProcessCallback?.Invoke(rec.ToTraceRow(TimePrecison));
                                break;
                            case RecordType.MessageData:
                                break;
                            default:
                                break;
                        }
                    ProgressCallback?.Invoke((int)dr.GetProgress);
                }
            }

            ProgressCallback?.Invoke(100);
        }

        public TraceCollection ToTraceList(Action<object> ProgressCallback = null)
        {
            TraceCollection TraceList = new TraceCollection();
            TraceList.StartLogTime = DatalogStartTime;
            ProcessTraceRecords((tc) => TraceList.AddRange(tc), ProgressCallback);
            return TraceList;
        }

        public bool ToXML(string xmlFileName)
        {
            XElement CreateBlock(XmlHandler xml, BinBase bin)
            {
                if (bin is null)
                    return null;
                object[] xmlAttr = new object[1] {
                     new XAttribute("UID", bin.header.uniqueid)

                };
                if (bin.external.ContainsKey("X"))
                {
                    xmlAttr = new object[3] {
                     new XAttribute("UID", bin.header.uniqueid),
                     new XAttribute("X", bin.external["X"]),
                     new XAttribute("Y", bin.external["Y"])
                    };
                }
                XElement xblock = xml.NewElement(bin.header.type.ToString().ToUpper(), xmlAttr);
                XmlSchemaComplexType xsdBinType = xml.xsdNodeType(xblock);

                foreach (KeyValuePair<string, PropertyData> prop in bin.data.Union(bin.external).Where(p => p.Value.XmlSequenceGroup == string.Empty))
                    if (!bin.data.isHelperProperty(prop.Value.Name))
                    {
                        if (prop.Value.PropType.IsArray)
                        {
                            // Check if it is sequence
                            XmlSchemaElement xsdPropType = xml.xsdObjectProperty(xsdBinType, prop.Value.Name + "_LIST");
                            if (xsdPropType is not null)
                            {
                                XElement seqblock = xml.NewElement(prop.Value.Name + "_LIST");
                                xblock.Add(seqblock);
                                foreach (var el in prop.Value.Value as Array)
                                    seqblock.Add(xml.NewElement(prop.Value.Name, el));
                            }
                            else
                            {
                                xsdPropType = xml.xsdObjectProperty(xsdBinType, prop.Value.Name);
                                if (xsdPropType is not null)
                                {
                                    if (xsdPropType.SchemaTypeName.Name == "hexBinary")
                                        xblock.Add(xml.NewElement(prop.Value.Name, xml.ToHexBytes(prop.Value.Value)));
                                }
                            }
                            /*XElement arr = new XElement(prop.Value.Name);
                            for (int i = 0; i < prop.Value.SubElementCount.Value; i++)
                                arr.Add(new XElement("SubElement", new XAttribute("ID", i + 1), prop.Value.Value[i]));
                            xblock.Add(arr);*/
                        }
                        else
                        {
                            XmlSchemaElement xsdPropType = xml.xsdObjectProperty(xsdBinType, prop.Value.Name);
                            if (xsdPropType is null)
                                continue;

                            xblock.Add(xml.NewElement(prop.Value.Name, prop.Value.Value));
                        }
                    }

                // XML Sequence grouping
                Dictionary<string, PropertyData[]> SequenceGroups =
                    bin.data.Union(bin.external).
                    Where(p => p.Value.XmlSequenceGroup != string.Empty).
                    GroupBy(p => p.Value.XmlSequenceGroup).
                    ToDictionary(p => p.Key, p => p.Where(pf => pf.Value.PropType.IsArray).Select(s => s.Value).ToArray());
                foreach (var seq in SequenceGroups)
                {
                    // Check if it is sequence
                    XmlSchemaElement xsdPropType = xml.xsdObjectProperty(xsdBinType, seq.Key + "_LIST");
                    if (xsdPropType is null)
                        continue;

                    XElement xmlSeqListBlock = xml.NewElement(seq.Key + "_LIST");
                    xblock.Add(xmlSeqListBlock);
                    int seqlen = seq.Value.Min(s => (s.Value as Array).Length);
                    for (int i = 0; i < seqlen; i++)
                    {
                        XElement xmlSeqEl = xml.NewElement(seq.Key);
                        xmlSeqListBlock.Add(xmlSeqEl);
                        foreach (var prop in seq.Value)
                            xmlSeqEl.Add(xml.NewElement(prop.Name, prop.Value[i]));
                    }
                }

                return xblock;
            }

            try
            {
                using (XmlHandler xml = new XmlHandler(xmlFileName))
                {
                    xml.CreateRoot("1.0.1",
                        new XElement[]
                        {
                            CreateBlock(xml, Config),
                            CreateBlock(xml, ConfigFTP),
                            CreateBlock(xml, ConfigMobile),
                            CreateBlock(xml, ConfigS3)
                        });

                    XElement groupNode;
                    foreach (var binGroup in this.GroupBy(x => x.Value.BinType))
                    {
                        groupNode = xml.AddGroupNode(binGroup.Key.ToString());
                        foreach (var bin in binGroup)
                            groupNode.Add(CreateBlock(xml, bin.Value));
                    }

                    xml.Save();
                }
                return true;
            }
            catch (Exception exc)
            {

                return false;
            }
        }

        private protected bool ReadXmlContent(XmlHandler xml)
        {
            BinBase ReadBin(XmlHandler xml, XElement node)
            {
                if (node is null)
                    return null;

                BinHeader hs = new BinHeader();
                if (!Enum.TryParse(node.Name.LocalName, true, out hs.type))
                    return null;

                if (!UInt16.TryParse(node.Attribute("UID").Value, out hs.uniqueid))
                    return null;

                BinBase bin = (BinBase)Activator.CreateInstance(BinBase.BlockInfo[hs.type], hs);
                XmlSchemaComplexType xsdBinType = xml.xsdNodeType(node);

                if (node.Attribute("X") is not null)
                    if (uint.TryParse(node.Attribute("X").Value, out uint X))
                        bin.external.AddProperty("X", typeof(uint), X);
                if (node.Attribute("Y") is not null)
                    if (uint.TryParse(node.Attribute("Y").Value, out uint Y))
                        bin.external.AddProperty("Y", typeof(uint), Y);
                foreach (var prop in bin.data.Where(p => p.Value.XmlSequenceGroup == string.Empty))
                {
                    if (prop.Value.PropType.IsArray)
                    {
                        // Check if it is sequence
                        XmlSchemaElement xsdPropType = xml.xsdObjectProperty(xsdBinType, prop.Value.Name + "_LIST");
                        if (xsdPropType is not null)
                        {
                            XElement propEl = XmlHandler.Child(node, prop.Value.Name + "_LIST");
                            if (propEl == null)
                                continue;

                            var converter = TypeDescriptor.GetConverter(prop.Value.PropType.GetElementType());
                            var arrElements = XmlHandler.Childs(propEl, prop.Value.Name);
                            var arrProp = Activator.CreateInstance(prop.Value.PropType, arrElements.Count());
                            for (int i = 0; i < arrElements.Count(); i++)
                                (arrProp as Array).SetValue(converter.ConvertFrom(arrElements.ElementAt(i).Value), i);
                            prop.Value.Value = arrProp;
                        }
                        else
                        {
                            XElement propEl = XmlHandler.Child(node, prop.Value.Name);
                            if (propEl == null)
                                continue;

                            xsdPropType = xml.xsdObjectProperty(xsdBinType, prop.Value.Name);
                            if (xsdPropType is not null)
                            {
                                if (xsdPropType.SchemaTypeName.Name == "hexBinary")
                                    prop.Value.Value = Bytes.FromHexBinary(propEl.Value);
                            }
                        }
                    }
                    else
                    {
                        XElement propEl = XmlHandler.Child(node, prop.Value.Name);
                        if (propEl == null)
                            continue;

                        var converter = TypeDescriptor.GetConverter(prop.Value.PropType);
                        prop.Value.Value = converter.ConvertFrom(propEl.Value);
                    }
                }

                // XML Sequence grouping
                Dictionary<string, PropertyData[]> SequenceGroups = bin.data.
                    Where(p => p.Value.XmlSequenceGroup != string.Empty).
                    GroupBy(p => p.Value.XmlSequenceGroup).
                    ToDictionary(p => p.Key, p => p.Where(pf => pf.Value.PropType.IsArray).Select(s => s.Value).ToArray());
                foreach (var seq in SequenceGroups)
                {
                    // Check if it is sequence
                    XmlSchemaElement xsdPropType = xml.xsdObjectProperty(xsdBinType, seq.Key + "_LIST");
                    if (xsdPropType is null)
                        continue;

                    XElement xmlSeqListBlock = XmlHandler.Child(node, seq.Key + "_LIST");
                    if (xmlSeqListBlock is null)
                        continue;

                    var arrElements = XmlHandler.Childs(xmlSeqListBlock, seq.Key);
                    int seqlen = arrElements.Count();
                    foreach (var prop in seq.Value)
                    {
                        var converter = TypeDescriptor.GetConverter(prop.PropType.GetElementType());
                        var arrProp = Activator.CreateInstance(prop.PropType, seqlen);

                        for (int i = 0; i < seqlen; i++)
                        {
                            XElement propEl = XmlHandler.Child(arrElements.ElementAt(i), prop.Name);
                            if (propEl is not null)
                                (arrProp as Array).SetValue(converter.ConvertFrom(propEl.Value), i);
                        }
                        prop.Value = arrProp;
                    }
                    seq.Value[0].SubElementCount.Value = seqlen;
                }

                var extProps = node.Elements().Where(n => !n.Name.LocalName.EndsWith("_LIST") && !bin.data.ContainsKey(n.Name.LocalName));
                foreach (var prop in extProps)
                {
                    bin.external.AddProperty(prop.Name.LocalName, typeof(string));
                    bin.external[prop.Name.LocalName] = prop.Value;
                }

                return bin;
            }

            BinBase ReadConfigBin(XmlHandler xml, Blocks.BlockType blocktype) => ReadBin(xml, XmlHandler.Child(xml.rootNode, blocktype.ToString().ToUpper()));

            if (xml.TryLoadXML(out Error))
            {
                Config = (BinConfig)ReadBin(xml, xml.configNode);
                ConfigFTP = (BinConfigFTP)ReadConfigBin(xml, Blocks.BlockType.Config_Ftp);
                ConfigMobile = (BinConfigMobile)ReadConfigBin(xml, Blocks.BlockType.Config_Mobile);
                ConfigS3 = (BinConfigS3)ReadConfigBin(xml, Blocks.BlockType.CONFIG_S3);

                foreach (XElement group in xml.blocksNode.Elements())
                    foreach (XElement bin in group.Elements())
                        Add(ReadBin(xml, bin));

                return true;
            }
            return false;
        }

        public bool ReadXMLStructure(Stream xmlData, Stream xsdData)
        {
            using (XmlHandler xml = new XmlHandler(xmlData, xsdData))
                return ReadXmlContent(xml);
        }

        public bool ReadXMLStructure(string xmlFileName)
        {
            using (XmlHandler xml = new XmlHandler(xmlFileName))
                return ReadXmlContent(xml);
        }

    }
}
