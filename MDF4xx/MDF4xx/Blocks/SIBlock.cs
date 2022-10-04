using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    using LinkEnum = SILinks;
    enum SILinks
    {
        /// <summary>
        /// Pointer to TXBLOCK with name (identification) of source (can be NIL). The source name must be according to naming rules stated in Naming Rules.
        /// </summary>
        si_tx_name,
        /// <summary>
        /// Pointer to TXBLOCK with (tool-specific) path of source (can be NIL). The path string must be according to naming rules stated in Naming Rules.
        /// Each tool may generate a different path string. The only purpose is to ensure uniqueness as explained in section 4.4.3 Identification of Channels.
        /// As a recommendation, the path should be a human readable string containing additional information about the source. However, the path string should 
        /// not be used to store this information in order to retrieve it later by parsing the string. Instead, additional source information should be stored in 
        /// generic or custom XML fields in the comment MDBLOCK si_md_comment.
        /// </summary>
        si_tx_path,
        /// <summary>
        /// Pointer to source comment and additional information (TXBLOCK or MDBLOCK) (can be NIL)
        /// </summary>
        si_md_comment,
        linkcount
    };

    enum SIType : byte
    {
        /// <summary>
        /// 0 = OTHER source type does not fit into given categories or is unknown 
        /// </summary>
        Other,
        /// <summary>
        /// 1 = ECU source is an ECU 
        /// </summary>
        ECU,
        /// <summary>
        /// 2 = BUS source is a bus(e.g. for bus monitoring) 
        /// </summary>
        BUS,
        /// <summary>
        /// 3 = I/O source is an I/O device(e.g.analog I/O) 
        /// </summary>
        IO,
        /// <summary>
        /// 4 = TOOL source is a software tool(e.g. for tool generated signals/events) 
        /// </summary>
        Tool,
        /// <summary>
        /// 5 = USER source is a user interaction/input(e.g. for user generated events)
        /// </summary>
        User
    }

    enum SIBusType : byte
    {
        /// <summary>
        /// 0 = NONE no bus
        /// </summary>
        None,
        /// <summary>
        /// 1 = OTHER bus type does not fit into given categories or is unknown
        /// </summary>
        Other,
        /// <summary>
        /// 2 = CAN
        /// </summary>
        CAN,
        /// <summary>
        /// 3 = LIN
        /// </summary>
        LIN,
        /// <summary>
        /// 4 = MOST
        /// </summary>
        Most,
        /// <summary>
        /// 5 = FLEXRAY
        /// </summary>
        FlexRay,
        /// <summary>
        /// 6 = K_LINE
        /// </summary>
        KLine,
        /// <summary>
        /// 7 = ETHERNET
        /// </summary>
        Ethernet,
        /// <summary>
        /// 8 = USB
        /// </summary>
        USB
    }

    /// <summary>
    /// Source Information Block
    /// </summary>
    class SIBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Source type - additional classification of source
            /// </summary>
            public SIType si_type;

            /// <summary>
            /// Bus type - additional classification of used bus(should be 0 for si_type ≥ 3) :
            /// Vendor defined bus types can be added starting with value 128.
            /// </summary>
            public SIBusType si_bus_type;

            /// <summary>
            /// Flags - The value contains the following bit flags(Bit 0 = LSB) :
            /// <br/>Bit 0: simulated source - Source is only a simulation(can be hardware or software simulated) Cannot be set for si_type = 4 (TOOL).
            /// </summary>
            public byte si_flags;

            /// <summary>
            /// Reserved
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 5)]
            byte[] si_reserved;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        // Objects to direct access childs
        public TXBlock tx_name => links.GetObject(LinkEnum.si_tx_name);
        public TXBlock tx_path => links.GetObject(LinkEnum.si_tx_path);
        public MDBlock md_comment => links.GetObject(LinkEnum.si_md_comment);

        public SIBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
        }
    };
}
