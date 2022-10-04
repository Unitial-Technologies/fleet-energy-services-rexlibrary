using InfluxShared.Generic;
using InfluxShared.Helpers;
using MDF4xx.Blocks;
using System;
using System.Collections.Generic;
using System.IO;

namespace MDF4xx.IO
{
    internal class MDF : BlockCollection
    {
        public static readonly string Extension = ".mf4";
        public static readonly string Filter = "ASAM MDF4 file (*.mf4)|*.mf4";

        string mdfFileName;
        public string FileName => mdfFileName;

        Int64 mdfFileSize = 0;
        public Int64 FileSize => mdfFileSize;

        Stream mdfStream;

        public MDF(string path = "") => mdfFileName = path;

        public MDF(Stream mdfStream) => this.mdfStream = mdfStream;

        public static MDF Open(string path)
        {
            MDF mdf = new MDF(path);

            using (FileStream stream = new FileStream(mdf.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader br = new BinaryReader(stream))
            {
                mdf.mdfFileSize = stream.Seek(0, SeekOrigin.End);
                stream.Seek(0, SeekOrigin.Begin);

                mdf.id = IDBlock.ReadBlock(br);

                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    Int64 fpos = br.BaseStream.Position;
                    BaseBlock block = BaseBlock.ReadNext(br);
                    block.flink = fpos;
                    mdf.Add(block);

                    if (!mdf.id.Finalized && block is DTBlock)
                        break;
                }
            }

            mdf.Init();

            return mdf;
        }

        public bool WriteHeader()
        {
            try
            {
                using (FileStream stream = new FileStream(FileName, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(stream))
                {
                    bw.Write(id.ToBytes());
                    foreach (KeyValuePair<Int64, BaseBlock> vp in this)
                    {
                        bw.Seek((int)vp.Key, SeekOrigin.Begin);
                        bw.Write(vp.Value.ToBytes());
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Finalize(string path)
        {
            if (Empty || Finalized)
                return false;

            if (id.data.id_custom_unfin_flags != 0)
                return false;

            DGBlock dg = hd.dg_first;
            using (FileStream sr = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                switch (dg.dg_data.Type)
                {
                    case BlockType.DT:
                        DTBlock dt = (DTBlock)dg.dg_data;
                        if (id.FlagDTLastLength)
                        {
                            dt.DataLength = FileSize - dt.flink - dt.DataOffset; // Update data block size
                        }

                        if (id.FlagCACGCycleCounters | id.FlagVlsdSize)
                        {
                            Dictionary<UInt64, CGBlock> cgdict = dg.GetGroupDict(id.FlagCACGCycleCounters);

                            sr.Seek(dt.flink + dt.DataOffset, SeekOrigin.Begin);

                            using PinObj rid = new PinObj(new byte[8]);
                            using PinObj vlsdSize = new PinObj(new byte[4]);
                            while (sr.FastRead(rid, dg.data.dg_rec_id_size) > 0)
                            {
                                if (cgdict.TryGetValue(rid, out CGBlock cg))
                                {
                                    if (id.FlagCACGCycleCounters)
                                        cg.data.cg_cycle_count++; // Update each CG cycle count
                                    if (cg.FlagVLSD)
                                    {
                                        sr.FastRead(vlsdSize, 4);
                                        sr.Seek(vlsdSize, SeekOrigin.Current);
                                        if (id.FlagVlsdSize)
                                            cg.data.cg_size.vlsd_size += vlsdSize;
                                    }
                                    else
                                    {
                                        sr.Seek(cg.data.cg_size.cg_data_bytes, SeekOrigin.Current);
                                    }
                                }
                            }
                        }
                        break;
                    case BlockType.DV:
                        break;
                    case BlockType.DZ:
                        break;
                    case BlockType.DL:
                        break;
                    case BlockType.LD:
                        break;
                    case BlockType.HL:
                        break;
                    default:
                        break;
                }

                id.Finalized = true;

                BlockCollection mdfobj = this;
                if (!Sorted)
                {
                    BlockCollection mdfcopy = SortedCopy();

                    DTBlock dt = (DTBlock)dg.dg_data;
                    mdfobj = mdfcopy;
                }

                using FileStream sw = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                byte[] wbuffer = mdfobj.id.ToBytes();
                sw.Write(wbuffer, 0, wbuffer.Length);
                foreach (KeyValuePair<Int64, BaseBlock> vp in mdfobj)
                {
                    sw.Seek((int)vp.Key, SeekOrigin.Begin);
                    wbuffer = vp.Value.ToBytes();
                    sw.Write(wbuffer, 0, wbuffer.Length);
                }
            }

            return true;
        }


    }
}
