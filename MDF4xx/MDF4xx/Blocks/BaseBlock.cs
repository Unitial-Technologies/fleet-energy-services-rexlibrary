using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    enum BlockType { Unknown, HD, MD, TX, FH, CH, AT, EV, DG, CG, SI, CN, CC, CA, DI, DT, DV, SR, RD, RI, RV, SD, DL, LD, DZ, HL };

    class BaseBlock
    {
        /// <summary>
        /// BlockType to Class reference dictionary
        /// </summary>
        static readonly Dictionary<BlockType, Type> BlockClass = new Dictionary<BlockType, Type>()
        {
            { BlockType.Unknown, typeof(BaseBlock) },
            { BlockType.HD, typeof(HDBlock) },
            { BlockType.MD, typeof(MDBlock) },
            { BlockType.TX, typeof(TXBlock) },
            { BlockType.FH, typeof(FHBlock) },
            { BlockType.CH, typeof(CHBlock) },
            { BlockType.AT, typeof(ATBlock) },
            { BlockType.EV, typeof(EVBlock) },
            { BlockType.DG, typeof(DGBlock) },
            { BlockType.CG, typeof(CGBlock) },
            { BlockType.SI, typeof(SIBlock) },
            { BlockType.CN, typeof(CNBlock) },
            { BlockType.CC, typeof(CCBlock) },
            { BlockType.CA, typeof(CABlock) },
            { BlockType.DI, typeof(DIBlock) },
            { BlockType.DT, typeof(DTBlock) },
            { BlockType.DV, typeof(DVBlock) },
            { BlockType.SR, typeof(SRBlock) },
            { BlockType.RD, typeof(RDBlock) },
            { BlockType.RI, typeof(RIBlock) },
            { BlockType.RV, typeof(RVBlock) },
            { BlockType.SD, typeof(SDBlock) },
            { BlockType.DL, typeof(DLBlock) },
            { BlockType.LD, typeof(LDBlock) },
            { BlockType.DZ, typeof(DZBlock) },
            { BlockType.HL, typeof(HLBlock) },
        };

        /// <summary>
        /// Header section
        /// </summary>
        internal HeaderSection header;

        internal class LinkObj
        {
            BaseBlock ParentObj;
            internal Int64[] FileLinks;
            internal BaseBlock[] LinkObjects;

            internal LinkObj(BaseBlock parent) => ParentObj = parent;

            public int Count
            {
                get => FileLinks.Length;
                set
                {
                    Array.Resize(ref FileLinks, value);
                    Array.Resize(ref LinkObjects, value);
                }
            }

            public Int64 this[Enum index] { get => this[Convert.ToInt32(index)]; set => this[Convert.ToInt32(index)] = value; }
            public Int64 this[int index]
            {
                get => (FileLinks.Length > index) ? FileLinks[index] : 0;
                set
                {
                    if (FileLinks.Length > index)
                        FileLinks[index] = value;
                }
            }

            public dynamic GetObject(Enum index) => GetObject(Convert.ToInt32(index));
            public dynamic GetObject(int index) => (LinkObjects.Length > index) ? LinkObjects[index] : null;
            public void SetObject(Enum index, BaseBlock block) => SetObject(Convert.ToInt32(index), block);
            public void SetObject(int index, BaseBlock block)
            {
                if (LinkObjects.Length <= index)
                    ParentObj.LinkCount = (UInt64)index + 1;
                LinkObjects[index] = block;
                FileLinks[index] = (block is null) ? 0 : block.flink;
            }

            public void ReplaceChildLink(Int64 oldlink, Int64 newlink)
            {
                for (int i = 0; i < Count; i++)
                    if (this[i] == oldlink)
                        this[i] = newlink;
            }

        }
        /// <summary>
        /// Links
        /// </summary>
        internal LinkObj links;

        /// <summary>
        /// Custom Data block
        /// </summary>
        internal object dataObj;
        internal int dataObjSize { get => (dataObj == null) ? 0 : Marshal.SizeOf(dataObj); }

        /// <summary>
        /// Variable Data block
        /// </summary>
        internal byte[] extraObj;
        internal virtual int extraObjSize { get => (extraObj == null) ? 0 : extraObj.Length; }

        /// <summary>
        /// Get whole block size
        /// </summary>
        public UInt64 Size => (UInt64)(Marshal.SizeOf(header) + links.Count * Marshal.SizeOf(typeof(UInt64)) + dataObjSize + extraObjSize);

        /// <summary>
        /// File link of current block
        /// </summary>
        public Int64 flink;

        /// <summary>
        /// Parent block
        /// </summary>
        public BaseBlock parent;

        public BaseBlock(HeaderSection hs = null)
        {
            header = hs;
            header ??= new HeaderSection();

            if (hs is null)
            {
                Type = DetectType();
            }

            links = new LinkObj(this);
        }

        public BlockType Type { get => header.Type; set => header.Type = value; }

        internal BlockType DetectType()
        {
            return BlockClass.FirstOrDefault(x => x.Value == GetType()).Key;
        }

        public UInt64 LinkCount
        {
            get => header.link_count;
            set
            {
                header.link_count = value;
                links.Count = (int)value;
            }
        }

        internal virtual void PostProcess() { }

        public virtual byte[] ToBytes() => ToBytes(false);
        public virtual byte[] ToBytes(bool SkipVariableData)
        {
            header.length = Size;

            byte[] buffer = new byte[header.length - (UInt64)(SkipVariableData ? extraObjSize : 0)];
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr p = h.AddrOfPinnedObject();
            Marshal.StructureToPtr(header, p, false);
            p += Marshal.SizeOf(header);
            Marshal.Copy(links.FileLinks, 0, p, links.Count);
            p += links.Count * Marshal.SizeOf(typeof(Int64));
            if (dataObj != null)
            {
                Marshal.StructureToPtr(dataObj, p, false);
            }
            if (extraObj != null)
            {
                p += dataObjSize;
                Marshal.Copy(extraObj, 0, p, extraObjSize);
            }
            h.Free();
            return buffer;
        }

        /// <summary>
        /// Read next block from BinaryReader and return object of specific class
        /// </summary>
        /// <param name="br">External BinaryReader</param>
        /// <returns>Inherited BaseBlock depending of block type</returns>
        public static BaseBlock ReadNext(BinaryReader br)
        {
            Int64 fstart = br.BaseStream.Position;

            // Read Header
            HeaderSection hs = HeaderSection.ReadBlock(br);
            BaseBlock block = (BaseBlock)Activator.CreateInstance(BlockClass[hs.Type], hs);

            // Read Links
            block.LinkCount = block.header.link_count;
            for (UInt64 i = 0; i < block.header.link_count; i++)
            {
                block.links.FileLinks[i] = br.ReadInt64();
            }

            // Read Data
            if (block.dataObj != null)
            {
                byte[] buffer = br.ReadBytes(Marshal.SizeOf(block.dataObj));
                GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.PtrToStructure(h.AddrOfPinnedObject(), block.dataObj);
                h.Free();
            }

            // Read Extra data
            UInt64 ecount = block.header.length - (UInt64)(br.BaseStream.Position - fstart);
            if (ecount > 0)
            {
                block.extraObj = br.ReadBytes((int)ecount);
            }

            // Skip alignment
            int align = (int)((8 - br.BaseStream.Position) & 7);
            if (align > 0)
            {
                br.ReadBytes(align);
            }

            // Post process
            block.PostProcess();

            return block;
        }

        public BaseBlock Clone()
        {
            byte[] data = ToBytes();
            MemoryStream ms = new MemoryStream(data);
            using (BinaryReader br = new BinaryReader(ms))
            {
                BaseBlock cblock = ReadNext(br);
                cblock.flink = flink;
                return cblock;
            }
        }

        public void SetWriteFileLink(ref Int64 writelink)
        {
            flink = writelink;
            writelink += (Int64)Size;
            Align(ref writelink);
        }

        public static void Align(ref Int64 value)
        {
            value = (value + 7) & ~7;
        }

        public override string ToString()
        {
            return Type.ToString() + " - " + header.length.ToString() + " bytes";
        }
    }
}

