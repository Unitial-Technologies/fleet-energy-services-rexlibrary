using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.HDLinks;

namespace MDF4xx.Blocks
{
    internal enum HDLinks
    {
        /// <summary>
        /// Pointer to the first data group block (DGBLOCK) (can be NIL)
        /// </summary>
        hd_dg_first,
        /// <summary>
        /// Pointer to first file history block (FHBLOCK)
        /// There must be at least one FHBLOCK with information about the application which created the MDF file.
        /// </summary>
        hd_fh_first,
        /// <summary>
        /// Pointer to first channel hierarchy block (CHBLOCK) (can be NIL).
        /// </summary>
        hd_ch_first,
        /// <summary>
        /// Pointer to first attachment block (ATBLOCK) (can be NIL)
        /// </summary>
        hd_at_first,
        /// <summary>
        /// Pointer to first event block (EVBLOCK) (can be NIL)
        /// </summary>
        hd_ev_first,
        /// <summary>
        /// Pointer to the measurement file comment (TXBLOCK or MDBLOCK) (can be NIL) For MDBLOCK contents, see Table 16.
        /// </summary>
        hd_md_comment,
        linkcount
    };

    /// <summary>
    /// Header Block
    /// </summary>
    internal class HDBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Time stamp at start of measurement in nanoseconds elapsed since 00:00:00 01.01.1970 
            /// (UTC time or local time, depending on "local time" flag, see[UTC]).
            /// All time stamps for time synchronized master channels or events are always relative to this	start time stamp.
            /// </summary>
            public UInt64 hd_start_time_ns;

            /// <summary>
            /// Time zone offset in minutes.
            /// The value is not necessarily a multiple of 60 and can be negative!For the current time zone 
            /// definitions, it is expected to be in the range[-840, 840] min.
            /// For example a value of 60 (min)means UTC + 1 time zone = Central European Time (CET).
            /// Only valid if "time offsets valid" flag is set in time flags.
            /// </summary>
            public Int16 hd_tz_offset_min;

            /// <summary>
            /// Daylight saving time(DST) offset in minutes	for start time stamp. During the summer
            /// months, most regions observe a DST offset of 60 min(1 hour).
            /// Only valid if "time offsets valid" flag is set in time flags.
            /// </summary>
            public Int16 hd_dst_offset_min;

            /// <summary>
            /// Time flags
            /// The value contains the following bit flags(Bit 0 = LSB) :
            /// Bit 0 : Local time flag
            /// If set, the start time stamp in nanoseconds	represents the local time instead of the UTC time,
            /// In this case, time zone and DST offset must not be considered(time offsets flag	must not be set).
            /// Should only be used if UTC time is unknown.	If the bit is not set(default), the start time stamp represents the UTC time.
            /// Bit 1: Time offsets valid flag
            /// If set, the time zone and DST offsets are valid.Must not be set together with "local time" flag (mutually exclusive).
            /// If the offsets are valid, the locally displayed	time at start of recording can be determined 
            /// (after conversion of offsets to ns) by Local time = UTC time + time zone offset + DST offset.
            /// </summary>
            public byte hd_time_flags;

            /// <summary>
            /// Time quality class
            /// 0 = local PC reference time(Default)
            /// 10 = external time source
            /// 16 = external absolute synchronized time
            /// </summary>
            public byte hd_time_class;

            /// <summary>
            /// Flags
            /// The value contains the following bit flags(Bit 0 = LSB) :
            /// Bit 0 : Start angle valid flag
            /// If set, the start angle value below is valid.
            /// Bit 1 : Start distance valid flag
            /// If set, the start distance value below is valid.
            /// </summary>
            public byte hd_flags;

            /// <summary>
            /// Reserved
            /// </summary>
            byte hd_reserved;

            /// <summary>
            /// Start angle in radians at start of measurement (only for angle synchronous measurements)
            /// Only valid if "start angle valid" flag is set. All angle values for angle synchronized
            /// master channels or events are relative to this start angle.
            /// </summary>
            public double hd_start_angle_rad;

            /// <summary>
            /// Start distance in meters at start of	measurement	(only for distance synchronous measurements)
            /// Only valid if "start distance valid" flag is set. All distance values for distance synchronized
            /// master channels or events are relative to this start distance.
            /// </summary>
            public double hd_start_distance_m;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public DGBlock dg_first => links.GetObject(LinkEnum.hd_dg_first);
        public FHBlock fh_first => links.GetObject(LinkEnum.hd_fh_first);
        public CHBlock ch_first => links.GetObject(LinkEnum.hd_ch_first);
        public ATBlock at_first => links.GetObject(LinkEnum.hd_at_first);
        public EVBlock ev_first => links.GetObject(LinkEnum.hd_ev_first);
        public MDBlock md_comment => links.GetObject(LinkEnum.hd_md_comment);

        public HDBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
