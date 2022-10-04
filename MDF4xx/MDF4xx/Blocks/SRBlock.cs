using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    using LinkEnum = SRLinks;
    enum SRLinks
    {
        /// <summary>
        /// Pointer to next sample reduction block (SRBLOCK) (can be NIL)
        /// </summary>
        sr_sr_next,
        /// <summary>
        /// Pointer to reduction data block with sample reduction records (RD-/RVBLOCK or DZBLOCK of this block type) or data list block for reduction data blocks 
        /// (DL-/LDBLOCK or HLBLOCK if required). RV- and LDBLOCK are optional for sorted groups, but required for column-oriented storage.
        /// </summary>
        sr_data,
        linkcount
    };

    /// <summary>
    /// Sample Reduction Block
    /// </summary>
    class SRBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Number of cycles, i.e. number of sample reduction records in the reduction data block.
            /// </summary>
            public UInt64 sr_cycle_count;

            /// <summary>
            /// Length of sample interval > 0 used to calculate the sample reduction records (see explanation below). Unit depends on sr_sync_type.
            /// </summary>
            public double sr_interval;

            /// <summary>
            /// Sync type
            /// <br/>1 = sr_interval contains time interval in seconds 
            /// <br/>2 = sr_interval contains angle interval in radians 
            /// <br/>3 = sr_interval contains distance interval in meter 
            /// <br/>4 = sr_interval contains index interval for record index
            /// <br/>See also section 4.4.6 Synchronization Domains.
            /// </summary>
            public byte sr_sync_type;

            /// <summary>
            /// Flags - The value contains the following bit flags(Bit 0 = LSB) : 
            /// <br/>Bit 0: invalidation Bytes flag - If set, the sample reduction record contains invalidation Bytes, i.e.after the three data Byte sections for mean, 
            /// minimum and maximum values, there is one invalidation Byte section. If not set, the invalidation Bytes are omitted.Must only be set if cg_inval_bytes > 0.
            /// <br/>Bit 1: dominant invalidation bit - If set, the invalidation bit for the sample reduction record must be set when any of the underlying raw records 
            /// is invalid (4.24.3 Variant 1). If not set, the invalidation bit for the sample reduction record must only be set when all of the underlying raw records 
            /// is invalid(4.24.3 Variant 2).
            /// <br/>Only valid if “invalidation Bytes flag” is set valid since MDF 4.2.0, should not be set for earlier versions
            /// </summary>
            public byte sr_flags;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 6)]
            byte[] sr_reserved;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public SRBlock sr_next => links.GetObject(LinkEnum.sr_sr_next);
        public BaseBlock sr_data => links.GetObject(LinkEnum.sr_data);

        public SRBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
