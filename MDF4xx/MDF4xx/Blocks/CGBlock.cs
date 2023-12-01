using InfluxShared.Helpers;
using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.CGLinks;

namespace MDF4xx.Blocks
{
    internal enum CGLinks
    {
        /// <summary>
        /// Pointer to next channel group block (CGBLOCK) (can be NIL)
        /// </summary>
        cg_cg_next,
        /// <summary>
        /// Pointer to first channel block (CNBLOCK) (can be NIL, must be NIL for VLSD CGBLOCK, i.e. if "VLSD channel group" flag (bit 0) is set)
        /// </summary>
        cg_cn_first,
        /// <summary>
        /// Pointer to acquisition name (TXBLOCK) (can be NIL, must be NIL for VLSD CGBLOCK)
        /// </summary>
        cg_tx_acq_name,
        /// <summary>
        /// Pointer to acquisition source (SIBLOCK) (can be NIL, must be NIL for VLSD CGBLOCK) See also rules for uniqueness explained in 4.4.3 Identification of Channels.
        /// </summary>
        cg_si_acq_source,
        /// <summary>
        /// Pointer to first sample reduction block (SRBLOCK) (can be NIL, must be NIL for VLSD CGBLOCK)
        /// </summary>
        cg_sr_first,
        /// <summary>
        /// Pointer to comment and additional information (TXBLOCK or MDBLOCK) (can be NIL, must be NIL for VLSD CGBLOCK)
        /// </summary>
        cg_md_comment,
        /*
        /// <summary>
        /// Only present if the “remote master” (bit 3) flag is set.
        /// This link points to another cg that must contain a master channel and must have the same cg_cycle_count.The channels in this channel group are to be treated as 
        /// if they were in the channel group linked by this link.
        /// This can be used to optimize for reading by splitting up the record into multiple smaller records. If a client then read one of the channels of this group, 
        /// not every signal value needs to be read. If each CGBLOCK contains only 1 channel, that group is said to be in column-oriented storage.
        /// The second use case for using this link is to add additional signals to the group (e.g.calculated signals during post-processing).
        /// Groups using that link must be stored either using LDBLOCKs or using a single DVBLOCK.They can only be sorted.
        /// Further details see: 4.14.3 Remote Master Link. Valid since MDF 4.2.0.
        /// </summary>
        cg_cg_master,*/
        linkcount
    }

    [Flags]
    internal enum CGFlags : UInt16
    {
        /// <summary>
        /// Bit 0: VLSD channel group flag.
        /// If set, this is a "variable length signal data" (VLSD) channel group.See explanation in 4.14.4 Variable Length Signal Data (VLSD) CGBLOCK.
        /// </summary>
        VLSD = 1 << 0,
        /// <summary>
        /// Bit 1: Bus event channel group flag.
        /// If set, this channel group contains information about a bus event, i.e. it contains a structure channel with bit 10 (bus event falg) set in cn_flags.
        /// For details please refer to MDF Bus Logging[7].
        /// valid since MDF 4.1.0, should not be set for earlier versions
        /// </summary>
        BusEvent = 1 << 1,
        /// <summary>
        /// Bit 2: Plain bus event channel group flag.
        /// Only relevant if "bus event channel group" flag(bit 1) is set.If set, this indicates that only the plain bus event is stored in this channel group, 
        /// but no channels describing the signals transported in the payload of the bus event. If not set, at least one channel for a signal transported in the 
        /// payload of the bus event (data frame/PDU) must be present. For details please refer to MDF Bus Logging [7].
        /// valid since MDF 4.1.0, should not be set for earlier versions
        /// </summary>
        PlainBusEvent = 1 << 2,
        /*
        /// <summary>
        /// Bit 3: Remote master flag.
        /// If set, this indicates that the channel group uses the master values of another channel group.That remote master group is linked by the cg_cg_master link.
        /// A group designated with this flag must be stored using LDBLOCKs or a single DVBLOCK.
        /// valid since MDF 4.2.0, should not be set for earlier versions.
        /// </summary>
        RemoteMaster = 1 << 3,
        /// <summary>
        /// Bit 4: Event signal group flag.
        /// If set, this indicates that the channel group is for storing events, not for storing measurement signals. See chapter 4.12.5 Event Signals for details.
        /// valid since MDF 4.2.0, should not be set for earlier versions
        /// </summary>
        EventSignal = 1 << 4*/
    }

    /// <summary>
    /// Channel Group Block
    /// </summary>
    internal class CGBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Record ID, value must be less than maximum unsigned integer value allowed by dg_rec_id_size in parent DGBLOCK. Record ID must be unique within linked list of CGBLOCKs.
            /// </summary>
            public UInt64 cg_record_id;

            /// <summary>
            /// Number of cycles, i.e. number of samples for this channel group. This specifies the number of records of this type in the data block.
            /// </summary>
            public UInt64 cg_cycle_count;

            /// <summary>
            /// Flags
            /// </summary>
            public CGFlags cg_flags;

            /// <summary>
            /// Value of character to be used as path separator, 0 if no path separator specified.
            /// The specified value is the UTF-16 Little Endian encoding of the character(no zero termination). Note: UTF-16 is used instead of UTF-8 to restrict the size to two Bytes.
            /// Commonly used characters are:
            /// - Dot(.) : 0x002E (dec: 46)
            /// - Slash(/) : 0x002F (dec: 47)
            /// - Backslash(\) : 0x005c (dec: 92)
            /// The path separator character can be specified if one of the following strings(see Table 9) is composed of several parts which are separated by the given character:
            /// - group name(gn), group source(gs), and group path(gp) for this channel group
            /// - channel name(cn), channel source(cs), and channel path(cp) for a channel in this channel group
            /// valid since MDF 4.1.0, should be 0 for earlier versions
            /// </summary>
            public UInt16 cg_path_separator;

            /// <summary>
            /// Reserved.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
            byte[] cg_reserved;

            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            internal struct CGSizeStruct
            {
                /// <summary>
                /// Normal CGBLOCK:
                /// Number of data Bytes(after record ID) used for signal values in record, i.e.size of plain data for each recorded sample of this channel group.
                /// VLSD CGBLOCK:
                /// Low part of a UINT64 value that specifies the total size in Bytes of all variable length signal values for the recorded samples of this channel group. 
                /// See explanation for cg_inval_bytes.
                /// </summary>
                [FieldOffset(0)]
                public UInt32 cg_data_bytes;

                /// <summary>
                /// Normal CGBLOCK:
                /// Number of additional Bytes for record used for invalidation bits.Can be zero if no invalidation bits are used at all.
                /// Invalidation bits may only occur in the specified number of Bytes after the data Bytes, not within the data Bytes that contain the signal values.
                /// VLSD CGBLOCK:
                /// High part of UINT64 value that specifies the total size in Bytes of all variable length signal values for the recorded samples of this channel group, 
                /// i.e.the total size in Bytes can be calculated by cg_data_bytes + (cg_inval_bytes shl 32) 
                /// Note: this value does not include the Bytes used to specify the length of each VLSD value!
                /// </summary>
                [FieldOffset(4)]
                public UInt32 cg_inval_bytes;

                [FieldOffset(0)]
                public UInt64 vlsd_size;
            }

            public CGSizeStruct cg_size;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public bool GetFlag(CGFlags flag) => data.cg_flags.HasFlag(flag);
        public void SetFlag(CGFlags flag, bool value) => data.cg_flags = data.cg_flags.SetFlag(flag, value);

        // CG Flags
        public bool FlagVLSD
        {
            get => GetFlag(CGFlags.VLSD);
            set => SetFlag(CGFlags.VLSD, value);
        }
        public bool FlagBusEvent
        {
            get => GetFlag(CGFlags.BusEvent);
            set => SetFlag(CGFlags.BusEvent, value);
        }
        public bool FlagPlainBusEvent
        {
            get => GetFlag(CGFlags.PlainBusEvent);
            set => SetFlag(CGFlags.PlainBusEvent, value);
        }
        /*public bool FlagRemoteMaster
        {
            get => GetFlag(CGFlags.RemoteMaster);
            set => SetFlag(CGFlags.RemoteMaster, value);
        }
        public bool FlagEventSignal
        {
            get => GetFlag(CGFlags.EventSignal);
            set => SetFlag(CGFlags.EventSignal, value);
        }*/

        // Objects to direct access childs
        public CGBlock cg_next => links.GetObject(LinkEnum.cg_cg_next);
        public CNBlock cn_first => links.GetObject(LinkEnum.cg_cn_first);
        public TXBlock tx_acq_name => links.GetObject(LinkEnum.cg_tx_acq_name);
        public SIBlock si_acq_source => links.GetObject(LinkEnum.cg_si_acq_source);
        public SRBlock sr_first => links.GetObject(LinkEnum.cg_sr_first);
        public MDBlock md_comment => links.GetObject(LinkEnum.cg_md_comment);
        //public CGBlock cg_master => links.GetObject(LinkEnum.cg_cg_master);

        public CNBlock time;

        public CGBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
            time = null;
        }

        public void Init()
        {
            //if (FlagRemoteMaster)
            {
                CNBlock cn = cn_first;
                while (cn != null)
                {
                    if (cn.data.cn_type == CNType.MasterChannel)
                    {
                        time = cn;
                        break;
                    }
                    cn = cn.cn_next;
                }
            }
            /*else
            {
                cg_master.Init();
                time = cg_master.time;
            }*/
        }

        public void AppendCN(CNBlock newcn)
        {
            if (cn_first is null)
            {
                links.SetObject(LinkEnum.cg_cn_first, newcn);
            }
            else
            {
                CNBlock cn = cn_first;
                while (cn.cn_next != null)
                    cn = cn.cn_next;

                cn.links.SetObject(CNLinks.cn_cn_next, newcn);
            }
        }

        public static CGBlock CreateCANDataFrame()
        {
            CGBlock cg = new CGBlock();
            return cg;
        }

    };
}
