using InfluxShared.FileObjects;
using MDF4xx.Blocks;
using System;
using System.Collections.Generic;

namespace MDF4xx.IO
{
    internal class BlockCollection : Dictionary<Int64, BaseBlock>
    {
        internal IDBlock id;
        internal HDBlock hd;
        internal List<BaseBlock> unlinked = new List<BaseBlock>();

        public bool Empty { get => Count == 0; }
        public UInt16 Version { get => (id is null) ? (UInt16)0 : id.Version; }
        public bool Finalized { get => (id is null) ? false : id.Finalized; }
        public bool Sorted = false;

        //internal Int64 FirstDataLink;

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

        public void BuildLoggerStruct(BlockBuilder builder, DateTime DatalogStartTime, Dictionary<UInt16, ChannelDescriptor> Signals = null, ExportCollections frameSignals = null)
        {
                builder.BuildID();
                builder.BuildHD(DatalogStartTime);
                builder.BuildFH();

                builder.BuildCanDataFrameGroup();
                builder.BuildCanErrorFrameGroup();
                builder.BuildLinDataFrameGroup();
                builder.BuildLinChecksumErrorFrameGroup();
                builder.BuildLinTransmissionErrorFrameGroup();

                if (Signals != null)
                    builder.BuildSignals(Signals);
                if (frameSignals != null)
                    builder.BuildFrameSignalGroups(frameSignals);
        }

    }
}
