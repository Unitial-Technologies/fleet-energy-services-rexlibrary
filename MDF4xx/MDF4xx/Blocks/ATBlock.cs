using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.ATLinks;

namespace MDF4xx.Blocks
{
    internal enum ATLinks
    {
        /// <summary>
        /// Link to next ATBLOCK (linked list) (can be NIL)
        /// </summary>
        at_at_next,
        /// <summary>
        /// Link to TXBLOCK with the path and file name of the embedded or referenced file (can only be NIL if data is embedded).
        /// The path of the file can be relative or absolute. If relative, it is relative to the directory of the MDF file. 
        /// If no path is given, the file must be in the same directory as the MDF file.
        /// </summary>
        at_tx_filename,
        /// <summary>
        /// LINK to TXBLOCK with MIME content-type text that gives information about the attached data. Can be NIL if the content-type is unknown, but should be specified whenever possible.
        /// The MIME content-type string must be written in lowercase.
        /// </summary>
        at_tx_mimetype,
        /// <summary>
        /// Link to MDBLOCK with comment and additional information about the attachment (can be NIL).
        /// </summary>
        at_md_comment,
        linkcount
    };

    /// <summary>
    /// Attachment Block
    /// </summary>
    internal class ATBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Flags - The value contains the following bit flags(Bit 0 = LSB) :
            /// <br/>Bit 0: Embedded data flag - If set, the attachment data is embedded, otherwise it is contained in an external file referenced by file path and name in at_tx_filename.
            /// <br/>Bit 1: Compressed embedded data flag - If set, the stream for the embedded data is compressed using the Defalte zip algorithm(see[12] and [24]).
            /// Can only be set if "embedded data" flag(bit 0) is set.
            /// <br/>Bit 2: MD5 check sum valid flag - If set, the at_md5_checksum field contains the MD5 check sum of the data.
            /// </summary>
            public UInt16 at_flags;

            /// <summary>
            /// Creator index, i.e. zero-based index of FHBLOCK in global list of FHBLOCKs that specifies which application has created this attachment, or changed it most recently.
            /// </summary>
            public UInt16 at_creator_index;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
            byte[] at_reserved;

            /// <summary>
            /// 128-bit value for MD5 check sum (of the uncompressed data if data is embedded and compressed). Only valid if "MD5 check sum valid" flag (bit 2) is set.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public byte[] at_md5_checksum;

            /// <summary>
            /// Original data size in Bytes, i.e. either for external file or for compressed data.
            /// </summary>
            public UInt64 at_original_size;

            /// <summary>
            /// Embedded data size N, i.e. number of Bytes for binary embedded data following this element. Must be 0 if external file is referenced.
            /// </summary>
            public UInt64 at_embedded_size;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        /// <summary>
        /// Contains binary embedded data (possibly compressed).
        /// </summary>
        public byte[] at_embedded_data { get => extraObj; set => extraObj = value; }

        // Objects to direct access childs
        public ATBlock at_next => links.GetObject(LinkEnum.at_at_next);
        public TXBlock tx_filename => links.GetObject(LinkEnum.at_tx_filename);
        public TXBlock tx_mimetype => links.GetObject(LinkEnum.at_tx_mimetype);
        public MDBlock md_comment => links.GetObject(LinkEnum.at_md_comment);

        public ATBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
