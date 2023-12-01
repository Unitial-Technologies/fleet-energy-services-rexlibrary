using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DTLinks;

namespace MDF4xx.Blocks
{
    internal enum DTLinks
    {
        linkcount
    };

    /// <summary>
    /// Data Block
    /// </summary>
    internal class DTBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public byte[] dt_data { get => extraObj; set => extraObj = value; }
        internal override int extraObjSize => (int)DataLength;

        // Data access
        public Int64 DataOffset;
        public Int64 DataLength
        {
            get => (Int64)(header.length - (UInt64)DataOffset);
            set => header.length = (UInt64)(value + DataOffset);
        }

        public DTBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;

            DataOffset = Marshal.SizeOf(header) + links.Count * Marshal.SizeOf(typeof(UInt64));
            //DataLength = (Int64)(header.length - (UInt64)DataOffset);
        }
    };
}
