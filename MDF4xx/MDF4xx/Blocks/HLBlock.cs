using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.HLLinks;

namespace MDF4xx.Blocks
{
    internal enum HLLinks
    {
        /// <summary>
        /// Pointer to the first data list block (DLBLOCK)
        /// </summary>
        hl_dl_first,
        linkcount
    };

    [Flags]
    internal enum HLFlags : UInt16
    {
        /// <summary>
        /// No flag is set
        /// </summary>
        None = 0,
        /// <summary>
        /// Bit 0: Equal length flag
        /// <br\>For the referenced DLBLOCK (and thus for each DLBLOCK in the linked list), the value of the "equal length" flag (bit 0 in dl_flags) must be equal to this flag.
        /// </summary>
        EqualLength = 1 << 0,
        /// <summary>
        /// Bit 1: Time values flag
        /// For the referenced DLBLOCK (and thus for each DLBLOCK in the linked list), the value of the "time values" flag (bit 1 in dl_flags) must be equal to this flag.
        /// </summary>
        TimeValues = 1 << 1,
        /// <summary>
        /// Bit 2: Angle values flag
        /// For the referenced DLBLOCK (and thus for each DLBLOCK in the linked list), the value of the "angle values" flag (bit 2 in dl_flags) must be equal to this flag.
        /// </summary>
        AngleValues = 1 << 2,
        /// <summary>
        /// Bit 3: Distance values flag
        /// For the referenced DLBLOCK (and thus for each DLBLOCK in the linked list), the value of the "distance values" flag (bit 3 in dl_flags) must be equal to this flag.
        /// </summary>
        DistanceValues = 1 << 3,
    }

    /// <summary>
    /// Header List Block
    /// </summary>
    internal class HLBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Flags - The value contains the following bit flags(Bit 0 = LSB) : 
            /// <br/>Bit 0: Equal length flag - For the referenced DLBLOCK(and thus for each DLBLOCK in the linked list), the value of the "equal length" flag(bit 0 in dl_flags) 
            /// must be equal to this flag.
            /// <br/>Bit 1: Time values flag - For the referenced DLBLOCK(and thus for each DLBLOCK in the linked list), the value of the "time values" flag(bit 1 in dl_flags) 
            /// must be equal to this flag.
            /// <br/>Bit 2: Angle values flag - For the referenced DLBLOCK(and thus for each DLBLOCK in the linked list), the value of the "angle values" flag(bit 2 in dl_flags) 
            /// must be equal to this flag.
            /// <br/>Bit 3: Distance values flag - For the referenced DLBLOCK(and thus for each DLBLOCK in the linked list), the value of the "distance values" flag(bit 3 in dl_flags) 
            /// must be equal to this flag.
            /// </summary>
            public HLFlags hl_flags;

            /// <summary>
            /// Zip algorithm used by DZBLOCKs referenced in the list, i.e. in an DLBLOCK of the link list starting at hl_dl_first.
            /// Note: all DZBLOCKs in the list must use the same zip algorithm.
            /// For possible values, please refer to dz_zip_type member of DZBLOCK.
            /// </summary>
            public byte hl_zip_type = 1;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 5)]
            byte[] hl_reserved;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public DLBlock dl_first => links.GetObject(LinkEnum.hl_dl_first);

        public HLBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
