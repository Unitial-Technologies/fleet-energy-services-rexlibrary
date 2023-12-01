using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DZLinks;

namespace MDF4xx.Blocks
{
    internal enum DZLinks
    {
        linkcount
    };

    /// <summary>
    /// Data Zipped Block
    /// </summary>
    internal class DZBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Block type identifier of the original(replaced) data block without the "##" prefix, i.e.either "DT", "SD", "RD" or "DV", "DI", "RV", "RI".
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 2)]
            public char[] dz_org_block;

            /// <summary>
            /// Zip algorithm used to compress the data stored in dz_data
            /// <br/>0 = Deflate The Deflate zip algorithm as used in various zip implementations(see[12] and [24])
            /// <br/>1 = Transposition + Deflate Before compression, the data block is transposed as explained in 4.31.2 Transposition of Data.
            /// <br/>Typically, only used for sorted data groups and DT-/DV-/DIBLOCK or RD-/RV-/RIBLOCK types.
            /// </summary>
            public byte dz_zip_type;

            /// <summary>
            /// Reserved
            /// </summary>
            byte dz_reserved;

            /// <summary>
            /// Parameter for zip algorithm. Content and meaning depends on dz_zip_type: For dz_zip_type = 1, the value must be > 1 and specifies the number of Bytes used as columns, i.e.usually the length of the record for a sorted data group.
            /// </summary>
            public UInt32 dz_zip_parameter;

            /// <summary>
            /// Length of uncompressed data in Bytes, i.e.length of data section for original data block.For a sorted data group, this should not exceed 222 Byte (4 MByte).
            /// </summary>
            public UInt64 dz_org_data_length;

            /// <summary>
            /// Length N of compressed data in Bytes, i.e.the number of Bytes stored in dz_data.
            /// </summary>
            public UInt64 dz_data_length;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public byte[] dz_data;
        internal override int extraObjSize => (int)DataLength;

        // Data access
        public Int64 DataOffset;
        public Int64 DataLength
        {
            get => (Int64)(header.length - (UInt64)DataOffset);
            set => header.length = (UInt64)(value + DataOffset);
        }

        public DZBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;

            data = new BlockData();
            DataOffset = Marshal.SizeOf(header) + links.Count * Marshal.SizeOf(typeof(UInt64)) + Marshal.SizeOf(data);
        }

    };
}
