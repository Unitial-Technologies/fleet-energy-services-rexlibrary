using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.CHLinks;

namespace MDF4xx.Blocks
{
    internal enum CHLinks
    {
        /// <summary>
        /// Link to next sibling CHBLOCK (can be NIL)
        /// </summary>
        ch_ch_next,
        /// <summary>
        /// Link to first child CHBLOCK (can be NIL, must be NIL for ch_type = 3 ("map list")).
        /// </summary>
        ch_ch_first,
        /// <summary>
        /// Link to TXBLOCK with the name of the hierarchy level. Must be NIL for ch_type ≥ 4, must not be NIL for all other types.
        /// If specified, the name must be according to naming rules stated in 4.4.2 Naming Rules, and it must be unique within all sibling CHBLOCKs.
        /// </summary>
        ch_tx_name,
        /// <summary>
        /// Link to TXBLOCK or MDBLOCK with comment and other information for the hierarchy level (can be NIL)
        /// </summary>
        ch_md_comment,
        linkcount
    };

    /// <summary>
    /// Channel Hierarchy Block
    /// </summary>
    internal class CHBlock : BaseBlock
    {
        public class HierarchyRecord
        {
            public Int64 dgBlock;
            public Int64 cgBlock;
            public Int64 cnBlock;
        }

        /// <summary>
        /// References to the channels for this hierarchy level.
        /// Each reference is a link triple with pointer to parent DGBLOCK, parent CGBLOCK and CNBLOCK for the channel.Thus the links have the following order
        /// DGBLOCK for channel 1 CGBLOCK for channel 1 CNBLOCK for channel 1 … DGBLOCK for channel N CGBLOCK for channel N CNBLOCK for channel N None of the links can be NIL.
        /// </summary>
        public HierarchyRecord ch_elementGet(int index) => new HierarchyRecord()
        {
            dgBlock = links[(int)LinkEnum.linkcount + index * 3],
            cgBlock = links[(int)LinkEnum.linkcount + 1 + index * 3],
            cnBlock = links[(int)LinkEnum.linkcount + 2 + index * 3]
        };
        public void ch_elementSet(int index, HierarchyRecord value)
        {
            links[(int)LinkEnum.linkcount + index * 3] = value.dgBlock;
            links[(int)LinkEnum.linkcount + 1 + index * 3] = value.cgBlock;
            links[(int)LinkEnum.linkcount + 2 + index * 3] = value.cnBlock;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Number of channels N referenced by this CHBLOCK.
            /// </summary>
            public UInt32 ch_element_count;

            /// <summary>
            /// Type of hierarchy level: (see also Table 21 for allowed child types)
            /// 0 = group
            /// All elements and children of this hierarchy level form a logical group(see[MCD - 2 MC] keyword GROUP).
            /// 1 = function
            /// All children of this hierarchy level form a functional group(see[MCD - 2 MC] keyword FUNCTION)
            /// For this type, the hierarchy must not contain CNBLOCK references(ch_element_count must be 0).
            /// 2 = structure
            /// All elements and children of this hierarchy level form a "fragmented" structure, see 5.18.1 Structures.
            /// Note: Do not use "fragmented" and "compact" structure in parallel.If possible prefer a "compact" structure.
            /// 3 = map list
            /// All elements of this hierarchy level form a map list (see[MCD - 2 MC] keyword MAP_LIST) :
            /// - the first element represents the z axis (must be a curve, i.e.CNBLOCK with CABLOCK of type "scaling axis")
            /// - all other elements represent the maps (must be 2 - dimensional map, i.e. CNBLOCK with CABLOCK of type "lookup")
            /// 4 = input variables of function (see[MCD - 2 MC] keyword IN_MEASUREMENT)
            /// All referenced channels must be measurement objects("calibration" flag(bit 7) not set in cn_flags)
            /// 5 = output variables of function (see[MCD - 2 MC] keyword OUT_MEASUREMENT)
            /// All referenced channels must be measurement objects("calibration" flag(bit 7) not set in cn_flags)
            /// 6 = local variables of function (see[MCD - 2 MC] keyword LOC_MEASUREMENT)
            /// All referenced channels must be measurement objects("calibration" flag(bit 7) not set in cn_flags)
            /// 7 = calibration objects defined in function (see[MCD - 2 MC] keyword DEF_CHARACTERISTIC)
            /// All referenced channels must be calibration objects ("calibration" flag(bit 7) set in cn_flags)
            /// 8 = calibration objects referenced in function (see[MCD - 2 MC] keyword REF_CHARACTERISTIC)
            /// All referenced channel must be calibration objects ("calibration" flag(bit 7) set in cn_flags)
            /// </summary>
            public byte ch_type;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 3)]
            byte[] ch_reserved;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public CHBlock ch_next => links.GetObject(LinkEnum.ch_ch_next);
        public CHBlock ch_first => links.GetObject(LinkEnum.ch_ch_first);
        public TXBlock tx_name => links.GetObject(LinkEnum.ch_tx_name);
        public MDBlock md_comment => links.GetObject(LinkEnum.ch_md_comment);

        public CHBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
