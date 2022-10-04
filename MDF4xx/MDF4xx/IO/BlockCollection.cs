using InfluxShared.FileObjects;
using MDF4xx.Blocks;
using MDF4xx.Frames;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MDF4xx.IO
{
    internal class BlockCollection : Dictionary<Int64, BaseBlock>
    {
        internal IDBlock id;
        internal HDBlock hd;

        public bool Empty { get => Count == 0; }
        public UInt16 Version { get => (id is null) ? (UInt16)0 : id.Version; }
        public bool Finalized { get => (id is null) ? false : id.Finalized; }
        public bool Sorted = false;

        public BlockCollection()
        {
            id = new IDBlock();

        }

        public void Add(BaseBlock block)
        {
            Add(block.flink, block);
        }

        void InitLinks()
        {
            foreach (KeyValuePair<Int64, BaseBlock> vp in this)
            {
                for (UInt64 i = 0; i < vp.Value.LinkCount; i++)
                {
                    if (TryGetValue(vp.Value.links[(int)i], out vp.Value.links.LinkObjects[i]))
                    {
                        vp.Value.links.LinkObjects[i].parent = vp.Value;
                    }
                }
            }

            hd = null;
            if (TryGetValue((Int64)id.Size, out BaseBlock bb))
            {
                hd = (HDBlock)bb;
            }
        }

        void UpdateSortStatus()
        {
            Sorted = false;

            DGBlock dg = hd.dg_first;
            while (dg != null)
            {
                if (dg.cg_first.cg_next != null)
                    return;
                dg = dg.dg_next;
            }
            Sorted = true;
        }


        internal void Init()
        {
            InitLinks();

            DGBlock dg = hd.dg_first;
            while (dg != null)
            {
                CGBlock cg = dg.cg_first;
                while (cg != null)
                {
                    cg.Init();
                    cg = cg.cg_next;
                }
                dg = dg.dg_next;
            }

            UpdateSortStatus();
        }

        internal BlockCollection SortedCopy()
        {
            BlockCollection mdfcopy = new BlockCollection();
            foreach (KeyValuePair<Int64, BaseBlock> vp in this)
                mdfcopy.Add(vp.Key, vp.Value.Clone());
            mdfcopy.Init();


            BlockCollection packed = new BlockCollection();
            packed.id = id.Clone();
            Int64 lastlink = (Int64)id.Size;

            foreach (KeyValuePair<Int64, BaseBlock> vp in mdfcopy)
            {
                BaseBlock block = vp.Value;
                if (block is DTBlock)
                    continue;

                foreach (KeyValuePair<Int64, BaseBlock> blockobj in mdfcopy)
                    blockobj.Value.links.ReplaceChildLink(block.flink, lastlink);

                block.flink = lastlink;
                lastlink += (Int64)block.Size;
                BaseBlock.Align(ref lastlink);

                packed.Add(block.flink, block);
            }

            DGBlock dg = mdfcopy.hd.dg_first;
            while (dg != null)
            {
                CGBlock cg = dg.cg_first;
                bool first = true;

                while (cg != null)
                {
                    if (cg.FlagVLSD)
                    {
                        cg = cg.cg_next;
                        continue;
                    }

                    DGBlock dgnew;
                    if (first)
                    {
                        dgnew = dg;
                    }
                    else
                    {
                        dgnew = new DGBlock();
                        dgnew.flink = lastlink;
                        lastlink += (Int64)dgnew.Size;
                        BaseBlock.Align(ref lastlink);
                        packed.Add(dgnew.flink, dgnew);
                    }

                    DTBlock dtnew = new DTBlock();
                    dtnew.flink = lastlink;
                    if (cg.FlagVLSD)
                        dtnew.DataLength = (Int64)cg.data.cg_size.vlsd_size;
                    else
                        dtnew.DataLength = (Int64)cg.data.cg_cycle_count * cg.data.cg_size.cg_data_bytes;
                    lastlink += (Int64)dtnew.Size;
                    BaseBlock.Align(ref lastlink);
                    packed.Add(dtnew.flink, dtnew);

                    if (!first)
                    {
                        dgnew.links.SetObject(DGLinks.dg_dg_next, dg.dg_next);
                        dg.links.SetObject(DGLinks.dg_dg_next, dgnew);
                    }
                    dgnew.links.SetObject(DGLinks.dg_cg_first, cg);
                    dgnew.links.SetObject(DGLinks.dg_data, dtnew);
                    dgnew.parent = dg.parent;
                    dgnew.data.dg_rec_id_size = 0;
                    dg = dgnew;

                    cg = cg.cg_next;
                    if (first)
                        dg.cg_first.links.SetObject(CGLinks.cg_cg_next, null);
                    first = false;
                }

                dg = dg.dg_next;
            }

            return packed;
        }

        public void BuildLoggerStruct(DateTime DatalogStartTime, byte TimestampSize, UInt32 TimestampPrecision, bool UseCompression, Dictionary<UInt16, ChannelDescriptor> Signals = null, ExportCollections frameSignals = null)
        {
            using (BlockBuilder builder = new BlockBuilder(this, TimestampSize, TimestampPrecision))
            {
                builder.BuildID();
                builder.BuildHD(DatalogStartTime);
                builder.BuildFH();

                DGBlock dg = builder.BuildDG((byte)Marshal.SizeOf(Enum.GetUnderlyingType(typeof(FrameType))));
                builder.BuildCanDataFrameGroup(dg);
                builder.BuildCanErrorFrameGroup(dg);
                builder.BuildLinDataFrameGroup(dg);
                builder.BuildLinChecksumErrorFrameGroup(dg);
                builder.BuildLinTransmissionErrorFrameGroup(dg);

                if (Signals != null)
                    builder.BuildSignals(dg, Signals);
                if (frameSignals != null)
                    builder.BuildFrameSignalGroups(dg, frameSignals);

                if (UseCompression)
                    builder.BuildDZ(dg, 0);
                else
                    builder.BuildDT(dg, 0);
            }
        }
    }
}
