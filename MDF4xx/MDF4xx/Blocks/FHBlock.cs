using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    using LinkEnum = FHLinks;
    enum FHLinks
    {
        /// <summary>
        /// Link to next FHBLOCK (can be NIL if list finished)
        /// </summary>
        fh_fh_next,
        /// <summary>
        /// Link to MDBLOCK containing comment about the creation or modification of the MDF file.
        /// </summary>
        fh_md_comment,
        linkcount
    }

    /// <summary>
    /// File History Block
    /// </summary>
    class FHBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Time stamp at which the file has been changed / created(first entry) in nanoseconds
            /// elapsed since 00:00 : 00 01.01.1970 (UTC time or local time, depending on "local time" flag).
            /// </summary>
            public UInt64 fh_time_ns;

            /// <summary>
            /// Time zone offset in minutes.
            /// The value is not necessarily a multiple of 60 and can be negative!For the current time zone
            /// definitions, it is expected to be in the range [-840, 840] min.
            /// For example a value of 60 (min)means UTC + 1 time zone = Central European Time (CET).
            /// Only valid if "time offsets valid" flag is set in time flags.
            /// </summary>
            public Int16 fh_tz_offset_min;

            /// <summary>
            /// Daylight saving time (DST) offset in minutes for start time stamp.During the summer
            /// months, most regions observe a DST offset of 60 min(1 hour).
            /// Only valid if "time offsets valid" flag is set in time flags.
            /// </summary>
            public Int16 fh_dst_offset_min;

            /// <summary>
            /// Time Flags
            /// The value contains the following bit flags(Bit 0	= LSB) :
            /// Bit 0 : Local time flag
            /// If set, the start time stamp in nanoseconds represents the local time instead of the UTC	time, 
            /// In this case, time zone and DST offset must not be considered(time offsets flag must	not be set).
            /// Should only be used if UTC time is unknown.
            /// If not set(default), the start time stamp represents the UTC time.
            /// Bit 1: Time offsets valid flag 
            /// If set, the time zone and DST offsets are valid. Must not be set together with "local time" flag (mutually exclusive).
            /// If the offsets are valid, the locally displayed time at start of recording can be determined
            /// (after conversion of offsets to ns) by Local time = UTC time + time zone offset + DST offset.
            /// </summary>
            public byte fh_time_flags;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 3)]
            byte[] fh_reserved;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public FHBlock fh_next => links.GetObject(LinkEnum.fh_fh_next);
        public MDBlock md_comment => links.GetObject(LinkEnum.fh_md_comment);

        public FHBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
