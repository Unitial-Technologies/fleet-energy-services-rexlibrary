using InfluxShared.Generic;
using InfluxShared.Helpers;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace InfluxShared.FileObjects
{
    public class BLF : IDisposable
    {
        #region BLF structsures
        /// <summary>
        /// Structure that describes common information in BLF file (start part of BLF file)
        /// <br/>SizeOf = 144 (0x90)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct Header
        {
            /// <summary>
            /// BLF file signature ("LOGG")
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
            public char[] m_Signature;
            /// <summary>
            /// Size of "virtual" structure that may be formed by data from this one and used in binlog.dll (VBLFileStatisticsEx)
            /// </summary>
            public UInt32 m_StructureSize;
            /// <summary>
            /// Application ID
            /// </summary>
            public byte m_ApplicationID;
            /// <summary>
            /// Application major number (usually 0)
            /// </summary>
            public byte m_ApplicationMajor;
            /// <summary>
            /// Application minor number (usually 0)
            /// </summary>
            public byte m_ApplicationMinor;
            /// <summary>
            /// Application build number (usually 0)
            /// </summary>
            public byte m_ApplicationBuildNo;
            /// <summary>
            /// BL API major number (BL_MAJOR_NUMBER)
            /// </summary>
            public byte m_BinLogMajor;
            /// <summary>
            /// BL API minor number (BL_MINOR_NUMBER)
            /// </summary>
            public byte m_BinLogMinor;
            /// <summary>
            /// BL API build number (BL_BUILD_NUMBER)
            /// </summary>
            public byte m_BinLogBuild;
            /// <summary>
            /// BL API patch number (BL_PATCH_NUMBER)
            /// </summary>
            public byte m_BinLogPatch;
            /// <summary>
            /// File size
            /// </summary>
            public UInt64 m_FileSize;
            /// <summary>
            /// Uncompressed file size
            /// </summary>
            public UInt64 m_FileSizeUncompressed;
            /// <summary>
            /// Count of objects in the file
            /// </summary>
            public UInt32 m_CountOfObjects;
            /// <summary>
            /// Count of objects read
            /// </summary>
            public UInt32 m_CountOfObjectsRead;
            /// <summary>
            /// Start time
            /// </summary>
            public SYSTEMTIME m_TimeStart;
            /// <summary>
            /// End time
            /// </summary>
            public SYSTEMTIME m_TimeEnd;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 18)]
            public UInt32[] m_NotUSed;
        }

        enum ObjType : UInt32
        {
            UNKNOWN = 0,
            CAN_MESSAGE = 1,
            CAN_ERROR = 2,
            CAN_OVERLOAD = 3,
            CAN_STATISTIC = 4,
            APP_TRIGGER = 5,
            ENV_INTEGER = 6,
            ENV_DOUBLE = 7,
            ENV_STRING = 8,
            ENV_DATA = 9,
            LOG_CONTAINER = 10,
            LIN_MESSAGE = 11,
            LIN_CRC_ERROR = 12,
            LIN_DLC_INFO = 13,
            LIN_RCV_ERROR = 14,
            LIN_SND_ERROR = 15,
            LIN_SLV_TIMEOUT = 16,
            LIN_SCHED_MODCH = 17,
            LIN_SYN_ERROR = 18,
            LIN_BAUDRATE = 19,
            LIN_SLEEP = 20,
            LIN_WAKEUP = 21,
            MOST_SPY = 22,
            MOST_CTRL = 23,
            MOST_LIGHTLOCK = 24,
            MOST_STATISTIC = 25,
            FLEXRAY_DATA = 29,
            FLEXRAY_SYNC = 30,
            CAN_DRIVER_ERROR = 31,
            MOST_PKT = 32,
            MOST_PKT2 = 33,
            MOST_HWMODE = 34,
            MOST_REG = 35,
            MOST_GENREG = 36,
            MOST_NETSTATE = 37,
            MOST_DATALOST = 38,
            MOST_TRIGGER = 39,
            FLEXRAY_CYCLE = 40,
            FLEXRAY_MESSAGE = 41,
            LIN_CHECKSUM_INFO = 42,
            LIN_SPIKE_EVENT = 43,
            CAN_DRIVER_SYNC = 44,
            FLEXRAY_STATUS = 45,
            GPS_EVENT = 46,
            FLEXRAY_ERROR = 47,
            FLEXRAY_STATUS2 = 48,
            FLEXRAY_STARTCYCLE = 49,
            FLEXRAY_RCVMESSAGE = 50,
            REALTIMECLOCK = 51,
            LIN_STATISTIC = 54,
            J1708_MESSAGE = 55,
            J1708_VIRTUAL_MSG = 56,
            LIN_MESSAGE2 = 57,
            LIN_SND_ERROR2 = 58,
            LIN_SYN_ERROR2 = 59,
            LIN_CRC_ERROR2 = 60,
            LIN_RCV_ERROR2 = 61,
            LIN_WAKEUP2 = 62,
            LIN_SPIKE_EVENT2 = 63,
            LIN_LONG_DOM_SIG = 64,
            APP_TEXT = 65,
            FLEXRAY_RCVMESSAGE_EX = 66,
            MOST_STATISTICEX = 67,
            MOST_TXLIGHT = 68,
            MOST_ALLOCTAB = 69,
            MOST_STRESS = 70,
            ETHERNET_FRAME = 71,
            SYS_VARIABLE = 72,
            CAN_ERROR_EXT = 73,
            CAN_DRIVER_ERROR_EXT = 74,
            LIN_LONG_DOM_SIG2 = 75,
            MOST_150_MESSAGE = 76,
            MOST_150_PKT = 77,
            MOST_ETHERNET_PKT = 78,
            MOST_150_MESSAGE_FRAGMENT = 79,
            MOST_150_PKT_FRAGMENT = 80,
            MOST_ETHERNET_PKT_FRAGMENT = 81,
            MOST_SYSTEM_EVENT = 82,
            MOST_150_ALLOCTAB = 83,
            MOST_50_MESSAGE = 84,
            MOST_50_PKT = 85,
            CAN_MESSAGE2 = 86,
            LIN_UNEXPECTED_WAKEUP = 87,
            LIN_SHORT_OR_SLOW_RESPONSE = 88,
            LIN_DISTURBANCE_EVENT = 89,
            SERIAL_EVENT = 90,
            OVERRUN_ERROR = 91,
            EVENT_COMMENT = 92,
            WLAN_FRAME = 93,
            WLAN_STATISTIC = 94,
            MOST_ECL = 95,
            GLOBAL_MARKER = 96,
            AFDX_FRAME = 97,
            AFDX_STATISTIC = 98,
            KLINE_STATUSEVENT = 99,
            CAN_FD_MESSAGE = 100,
            CAN_FD_MESSAGE_64 = 101,
            ETHERNET_RX_ERROR = 102,
            ETHERNET_STATUS = 103,
            CAN_FD_ERROR_64 = 104,
            AFDX_STATUS = 106,
            AFDX_BUS_STATISTIC = 107,
            AFDX_ERROR_EVENT = 109,
            A429_ERROR = 110,
            A429_STATUS = 111,
            A429_BUS_STATISTIC = 112,
            A429_MESSAGE = 113,
            ETHERNET_STATISTIC = 114,
            TEST_STRUCTURE = 118,
            DIAG_REQUEST_INTERPRETATION = 119,
            ETHERNET_FRAME_EX = 120,
            ETHERNET_FRAME_FORWARDED = 121,
            ETHERNET_ERROR_EX = 122,
            ETHERNET_ERROR_FORWARDED = 123,
            FUNCTION_BUS = 124,
            DATA_LOST_BEGIN = 125,
            DATA_LOST_END = 126,
            WATER_MARK_EVENT = 127,
            TRIGGER_CONDITION = 128,
            CAN_SETTING_CHANGED = 129,
            DISTRIBUTED_OBJECT_MEMBER = 130,
            ATTRIBUTE_EVENT = 131,
        }

        /// <summary>
        /// Base part of BLF file object
        /// <br/>SizeOf = 16 (0x10)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct ObjectHeaderBase
        {
            /// <summary>
            /// Signature (BLF_OBJECT_SIGNATURE)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
            public char[] m_Signature;
            /// <summary>
            /// Size of this structure; sizeof(VBLObjectHeader)
            /// </summary>
            public UInt16 m_HeaderSize;
            /// <summary>
            /// Object header version (1)
            /// </summary>
            public UInt16 m_HeaderVersion;
            /// <summary>
            /// Object size
            /// </summary>
            public UInt32 m_ObjectSize;
            /// <summary>
            /// Object type (BLF_OBJECT_TYPE_XXX) 0x31 - cycle start 0x42 - fR
            /// </summary>
            public ObjType m_ObjectType;
        }

        /// <summary>
        /// Header of BLF object
        /// <br/>SizeOf = 32 (0x20)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct ObjectHeader
        {
            /// <summary>
            /// Base header
            /// </summary>
            public ObjectHeaderBase m_Header;
            /// <summary>
            /// Unit of object timestamp. Following values are possible:
            /// <br/>1: Object time stamp is saved as multiple of ten microseconds(BL_OBJ_FLAG_TIME_TEN_MICS)
            /// <br/>2: Object time stamp is saved in nanoseconds. (BL_OBJ_FLAG_TIME_ONE_NANS)
            /// </summary>
            public UInt32 m_Flags;
            public UInt16 m_NotUsed;
            /// <summary>
            /// Object specific version, has to be set to 0 unless stated otherwise in the description of a specific event.
            /// </summary>
            public UInt16 m_Version;
            /// <summary>
            /// Object timestamp
            /// </summary>
            public UInt64 m_TimeStamp;
        }

        /// <summary>
        /// Structure that describes log container in BLF file
        /// <br/>SizeOf = 32 (0x20)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LogContainer
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeaderBase m_Header;
            /// <summary>
            /// Flags (usually BL_OBJ_FLAG_TIME_ONE_NANS)
            /// </summary>
            public UInt32 m_Flags;
            /// <summary>
            /// Not used
            /// </summary>
            public UInt16 m_NotUsed;
            /// <summary>
            /// Object version (usually 0)
            /// </summary>
            public UInt16 m_Version;
            /// <summary>
            /// Uncompressed size of the object
            /// </summary>
            public UInt64 m_SizeUncompressed;
        }

        /// <summary>
        /// Structure that describes CAN message in BLF file
        /// <br/>SizeOf = 48 (0x30)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct CanMessage
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Channel no
            /// </summary>
            public UInt16 m_Channel;
            /// <summary>
            /// CAN dir & rtr
            /// </summary>
            public byte m_Flags;
            /// <summary>
            /// CAN message data length
            /// </summary>
            public byte m_DLC;
            /// <summary>
            /// CAN message ID
            /// </summary>
            public UInt32 m_ID;
            /// <summary>
            /// CAN message data
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
            public byte[] m_Data;
        }

        /// <summary>
        /// Structure that describes CAN Error in BLF file
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct CanError
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Channel no
            /// </summary>
            public UInt16 m_Channel;
            /// <summary>
            /// Length of error frame, unused, may be 0.
            /// </summary>
            public UInt16 m_Length;
            /// <summary>
            /// Defines what additional information is valid. Following values are possible:
            /// <br/>1: SJA 1000 ECC is valid(member mECC)
            /// <br/>2: Vector CAN Core Error Code is valid.
            /// <br/>4: Vector CAN Core Error Position
            /// <br/>8: Vector CAN Core Frame Length in ns
            /// </summary>
            public UInt32 m_Flags;
            /// <summary>
            /// Content of Philips SJA1000 Error Code Capture (ECC) register, or the Vector CAN-Core error register (see also mFlags).
            /// <br/>SJA1000-ECC. See documentation of Philips SJA1000 CAN Controller.
            /// <br/>Vector CAN-Core
            /// <br/>Bit Meaning
            /// <br/>0-5 0: Bit Error - 1: Form Error; 2: Stuff Error; 3: Other Error; 4: CRC Error; 5: Ack-Del-Error; 7: Ack-Error
            /// <br/>6-7 0: RX-NAK-Error - 1: TX-NAK-Error; 2: RX-Error; 3: TX-Error
            /// </summary>
            public byte m_ECC;

            /// <summary>
            /// Bit position of the error frame in the corrupted message.
            /// </summary>
            public byte m_Position;
            /// <summary>
            /// Data length code of the corrupted message.
            /// </summary>
            public byte m_DLC;
            /// <summary>
            /// Reserved
            /// </summary>
            public byte m_Reserved1;
            /// <summary>
            /// Difference between the time stamp of the error frame and the start of frame in nanoseconds. Not all hardware interfaces are supporting this parameter.
            /// </summary>
            public UInt32 m_FrameLengthInNS;
            /// <summary>
            /// Message ID of the corrupted message.
            /// </summary>
            public UInt32 m_ID;
            /// <summary>
            /// Extended error flags.
            /// <br/>Bit Meaning
            /// <br/>0-4: Segment (only SJA1000); 
            /// <br/>5: Direction, 1=RX
            /// <br/>6-11: Error Code - 0 Bit Error; 1 Form Error; 2 Stuff Error; 3 Other Error; 4 CRC Error; 5 ACK-DEL Error; 7 ACK Error
            /// <br/>12-13: Extended Direction - 0 RX NAK; 1 TX NAK; 2 RX; 3 TX
            /// <br/>14: 1 = The error frame was send from the application
            /// </summary>
            public UInt16 m_FlagsExt;
            /// <summary>
            /// Reserved, must be 0
            /// </summary>
            public UInt16 m_Reserved2;
            /// <summary>
            /// Message data
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
            public byte[] m_Data;
        }

        /// <summary>
        /// Structure that describes CAN message in BLF file
        /// <br/>SizeOf = 48 (0x30)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct CanFDMessage
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Channel no
            /// </summary>
            public byte m_Channel;
            /// <summary>
            /// CAN message data length
            /// </summary>
            public byte m_DLC;
            public byte m_ValidDataBytes;
            /// <summary>
            /// Bits 0 – 3: Number of required transmission attempts; Bits 4 – 7: Max number of transmission attempts.
            /// </summary>
            public byte m_TxCount;
            /// <summary>
            /// CAN message ID
            /// </summary>
            public UInt32 m_ID;
            /// <summary>
            /// Message duration [in ns]. Not including 3 interframe-space bit times and by Rx-messages also not including one end-of-frame bit time
            /// </summary>
            public UInt32 m_FrameLength;
            /// <summary>
            /// CAN dir & rtr
            /// </summary>
            public UInt32 m_Flags;
            /// <summary>
            /// CAN- or CAN-FD bit timing configuration for arbitration phase. Bit 0-7: Quartz frequency im MHz Bit 8-15: Prescaler Bit 16-23: # of time quanta per bit Bit 24-31: Sampling point in percent
            /// </summary>
            public UInt32 m_BtrCfgArb;
            /// <summary>
            /// CAN-FD bit timing configuration for data phase, may be 0, if not supported by hardware/driver. See mBtrCfgArb.
            /// </summary>
            public UInt32 m_BtrCfgData;
            /// <summary>
            /// Time offset of the sampling point of BRS in nanoseconds
            /// </summary>
            public UInt32 m_TimeOffsetBrsNs;
            /// <summary>
            /// Time offset of the sampling point of CRC delimiter in nanoseconds
            /// </summary>
            public UInt32 m_TimeOffsetCRCDelNs;
            /// <summary>
            /// Bit count of the message, exclusive stuff bits.
            /// </summary>
            public UInt16 m_BitCount;
            /// <summary>
            /// Direction of the message
            /// </summary>
            public byte m_Dir;
            public byte m_ExtDataOffset;
            public UInt32 m_CRC;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct FlexRayMessage
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Channel no
            /// </summary>
            public UInt16 m_Channel;
            /// <summary>
            /// Object version, for internal use - Value:1
            /// </summary>
            public UInt16 m_Version;
            /// <summary>
            /// See 3.3.2 - Value:2
            /// </summary>
            public UInt16 m_ChannelMask;
            /// <summary>
            /// See 3.3.1 - Value:0
            /// </summary>
            public UInt16 m_Dir;
            /// <summary>
            /// Client index of send node. Must be set to 0 if file is written from other applications.
            /// </summary>
            public UInt32 m_ClientIndex;
            /// <summary>
            /// Number of cluster: channel number - 1 - Value:1
            /// </summary>
            public UInt32 m_ClusterNo;
            /// <summary>
            /// Slot identifier
            /// </summary>
            public UInt16 m_FrameId;
            /// <summary>
            /// Header CRC FlexRay channel 1 (A)
            /// </summary>
            public UInt16 m_HeaderCRC1;
            /// <summary>
            /// Header CRC FlexRay channel 2 (B)
            /// </summary>
            public UInt16 m_HeaderCRC2;
            /// <summary>
            /// Payload length in bytes
            /// </summary>
            public UInt16 m_ByteCount;
            /// <summary>
            /// Number of bytes of the payload stored in mDataBytes. If the CC-frame buffer was too small to receive the complete payload, then mDataCount is smaller than mByteCount.
            /// </summary>
            public UInt16 m_DataCount;
            /// <summary>
            /// Cycle number
            /// </summary>
            public UInt16 m_Cycle;
            /// <summary>
            /// Type of communication controller, see 3.3.3 - Value:2
            /// </summary>
            public UInt32 m_Tag;
            /// <summary>
            /// Controller specific frame state information, see 3.3.4 - Value:1d0
            /// </summary>
            public UInt32 m_DataFrameState;
            /// <summary>
            /// See description of flags, see 3.3.5 - Value:2
            /// </summary>
            public UInt32 m_FrameFlags;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 11)]
            public UInt32[] m_NotUsed;
            /// <summary>
            /// Payload
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 256)]
            public byte[] m_Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinBusEvent
        {
            /// <summary>
            /// Timestamp of frame/event start
            /// </summary>
            public UInt64 m_SOF;
            /// <summary>
            /// Baudrate of frame/event in bit/sec
            /// </summary>
            public UInt32 m_EventBaudrate;
            /// <summary>
            /// Channel number where the frame/event notified
            /// </summary>
            public UInt16 m_Channel;
            /// <summary>
            /// Reserved, has to be set to 0.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 2)]
            public byte[] m_Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinSynchFieldEvent
        {
            /// <summary>
            /// Common LIN bus event header. See 0
            /// </summary>
            public LinBusEvent m_LinBusEvent;
            /// <summary>
            /// Length of dominant part [in nanoseconds]
            /// </summary>
            public UInt64 m_SynchBreakLength;
            /// <summary>
            /// Length of delimiter (recessive) [in nanoseconds]
            /// </summary>
            public UInt64 m_SynchDelLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinMessageDescriptor
        {
            /// <summary>
            /// Common LIN bus event header. See 3.1.5
            /// </summary>
            public LinSynchFieldEvent m_LinSynchFieldEvent;
            /// <summary>
            /// Supplier identifier of the frame’s transmitter as it is specified in LDF. LIN protocol 2.0 and higher
            /// </summary>
            public UInt16 m_SupplierID;
            /// <summary>
            /// LIN protocol 2.0: Message identifier (16-bit) of the frame as it is specified in LDF in the list of transmitter’s configurable frames.
            /// <br/>LIN protocol 2.1: Position index of the frame as it is specified in LDF in the list of transmitter’s configurable frames.
            /// </summary>
            public UInt16 m_MessageID;
            /// <summary>
            /// Configured Node Address of the frame’s transmitter as it is specified in LDF. LIN protocol 2.0 and higher
            /// </summary>
            public byte m_NAD;
            /// <summary>
            /// Frame identifier (6-bit)
            /// </summary>
            public byte m_ID;
            /// <summary>
            /// Frame length [in bytes]
            /// </summary>
            public byte m_DLC;
            /// <summary>
            /// Expected checksum model of checksum value. Only valid if mObjectVersion >= 1.
            /// </summary>
            public byte m_ChecksumModel;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinTimestampEvent
        {
            /// <summary>
            /// Common LIN bus event header. See 3.1.6
            /// </summary>
            public LinMessageDescriptor m_LinMsgDescrEvent;
            /// <summary>
            /// Data byte timestamps [in nanoseconds] Index 0 corresponds to last header byte Indexes 1-9 correspond to response data bytes D1-D8
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 9)]
            public UInt64[] m_DatabyteTimestamps;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinMessage2
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Common LIN bus event header. See 3.1.7
            /// </summary>
            public LinTimestampEvent m_LinTimestampEvent;
            /// <summary>
            /// Databyte values
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
            public byte[] m_Data;
            /// <summary>
            /// Checksum byte value
            /// </summary>
            public UInt16 m_CRC;
            /// <summary>
            /// See 3.1.8
            /// </summary>
            public byte m_Dir;
            /// <summary>
            /// Flag indicating whether this frame a simulated one: (0: real frame; 1: simulated frame)
            /// </summary>
            public byte m_Simulated;
            /// <summary>
            /// Flag indicating whether this frame is Event- Triggered one: (0: not ETF; 1: ETF)
            /// </summary>
            public byte m_IsETF;
            /// <summary>
            /// Event-Triggered frame only: Index of associated frame, which data is carried
            /// </summary>
            public byte m_ETFAssocIndex;
            /// <summary>
            /// Event-Triggered frame only: Frame identifier (6-bit) of associated frame, which data is carried
            /// </summary>
            public byte m_ETFAssocETFId;
            /// <summary>
            /// Slave Identifier in the Final State Machine (obsolete)
            /// </summary>
            public byte m_FSMId;
            /// <summary>
            /// State Identifier of a Slave in the Final State Machine (obsolete)
            /// </summary>
            public byte m_FSMState;
            /// <summary>
            /// Reserved, has to be set to 0.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 3)]
            public byte[] m_Reserved;
            /// <summary>
            /// Event’s baudrate measured in response [in bits/sec]
            /// </summary>
            public UInt32 m_RespBaudrate;
            /// <summary>
            /// Event’s baudrate measured in header [in bits/sec]
            /// </summary>
            public double m_ExactHeaderBaudrate;
            /// <summary>
            /// Early stop bit offset in frame header for UART timestamps [in ns]
            /// </summary>
            public UInt32 m_EarlyStopbitOffset;
            /// <summary>
            /// Early stop bit offset in frame response for UART timestamps [in ns]
            /// </summary>
            public UInt32 m_EarlyStopbitOffsetResponse;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinCrcError
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Channel number where the event notified
            /// </summary>
            public UInt16 m_Channel;
            /// <summary>
            /// Frame identifier
            /// </summary>
            public byte m_ID;
            /// <summary>
            /// Frame length
            /// </summary>
            public byte m_DLC;
            /// <summary>
            /// Databyte values
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
            public byte[] m_Data;
            /// <summary>
            /// Slave Identifier in the Final State Machine (obsolete)
            /// </summary>
            public byte m_FSMId;
            /// <summary>
            /// State Identifier of a Slave in the Final State Machine (obsolete)
            /// </summary>
            public byte m_FSMState;
            /// <summary>
            /// Duration of the frame header [in bit times]
            /// </summary>
            public byte m_HeaderTime;
            /// <summary>
            /// Duration of the entire frame [in bit times]
            /// </summary>
            public byte m_FullTime;
            /// <summary>
            /// Checksum byte value
            /// </summary>
            public UInt16 m_CRC;
            /// <summary>
            /// See 3.1.8
            /// </summary>
            public byte m_Dir;
            /// <summary>
            /// Reserved, has to be set to 0.
            /// </summary>
            public byte m_Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct LinSendError
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Channel number where the event notified
            /// </summary>
            public UInt16 m_Channel;
            /// <summary>
            /// Frame identifier
            /// </summary>
            public byte m_ID;
            /// <summary>
            /// Frame length
            /// </summary>
            public byte m_DLC;
            /// <summary>
            /// Slave Identifier in the Final State Machine (obsolete)
            /// </summary>
            public byte m_FSMId;
            /// <summary>
            /// State Identifier of a Slave in the Final State Machine (obsolete)
            /// </summary>
            public byte m_FSMState;
            /// <summary>
            /// Duration of the frame header [in bit times]
            /// </summary>
            public byte m_HeaderTime;
            /// <summary>
            /// Duration of the entire frame [in bit times]
            /// </summary>
            public byte m_FullTime;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct SystemVariable
        {
            /// <summary>
            /// BLF object header
            /// </summary>
            public ObjectHeader m_Header;
            /// <summary>
            /// Type of system variable. Following values are possible:
            /// <br/>1: DOUBLE (BL_SYSVAR_TYPE_DOUBLE)
            /// <br/>2: LONG (BL_SYSVAR_TYPE_LONG)
            /// <br/>3: STRING (BL_SYSVAR_TYPE_STRING)
            /// <br/>4: Array of DOUBLE (BL_SYSVAR_TYPE_DOUBLEARRAY)
            /// <br/>5: Array of LONG (BL_SYSVAR_TYPE_LONGARRAY)
            /// <br/>6: LONGLONG (BL_SYSVAR_TYPE_LONGLONG)
            /// <br/>7: Array of BYTE (BL_SYSVAR_TYPE_BYTEARRAY)
            /// </summary>
            public UInt32 m_Type;
            /// <summary>
            /// Reserved, must be 0.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 3)]
            public UInt32[] m_Reserved;
            /// <summary>
            /// Length of the name of the system variable (without terminating 0)
            /// </summary>
            public UInt32 m_NameLength;
            /// <summary>
            /// Length of the data of the environment variable in bytes.
            /// </summary>
            public UInt32 m_DataLength;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
            public byte[] m_Reserved2;
            /// <summary>
            /// Name of the system variable.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 21)]
            public char[] m_Name;
            /// <summary>
            /// Data value of the system variable.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 256)]
            public byte[] m_Data;
        }
        #endregion

        public static readonly string Extension = ".blf";
        public static readonly string Filter = "Vector binary frames (*.blf)|*.blf";

        const UInt32 BufferSize = 0xA00000;
        private static readonly byte[] padding = new byte[4] { 0, 0, 0, 0 };
        public static readonly List<byte> DlcFDList = new List<byte> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 12, 16, 20, 24, 32, 48, 64 };
        private static readonly List<byte> AsamErrorMap = new List<byte> { 3, 0, 1, 2, 4, 7 };
        static readonly List<string> SystemVariableNames = new List<string>
        {
            "::Influx::Rebel::DIN0",
            "::Influx::Rebel::DIN1",
            "::Influx::Rebel::DIN2",
            "::Influx::Rebel::DIN3",
            "::Influx::Rebel::AIN0",
            "::Influx::Rebel::AIN1",
            "::Influx::Rebel::AIN2",
            "::Influx::Rebel::AIN3",
        };

        Stream fs = null;
        BinaryWriter bw = null;
        UInt32 ObjectsCount = 0;
        UInt64 ObjectsSize = 0;
        private bool disposedValue;
        byte[] buffer = new byte[BufferSize];
        UInt32 bufferPos = 0;
        private DateTime DatalogTime = DateTime.Now;

        public BLF()
        {
        }

        #region Destructors

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BLF()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public static byte VectorError(byte AsamError, bool IsRx = true) =>
            (byte)(AsamErrorMap[AsamError >= AsamErrorMap.Count ? 0 : AsamError] | ((IsRx ? 2 : 3) << 6));

        public static UInt16 VectorErrorExt(byte AsamError, bool IsRx = true)=>
            (UInt16)((VectorError(AsamError, IsRx) << 6) | ((IsRx ? 1 : 0) << 5));

        public bool CreateFile(string FileName, DateTime LogTime)
        {
            fs = new FileStream(FileName, FileMode.Create);
            return CreateStream(fs, LogTime);
        }

        public bool CreateStream(Stream blfStream, DateTime LogTime)
        {
            try
            {
                DatalogTime = LogTime;
                ObjectsCount = 0;
                ObjectsSize = 0;
                bufferPos = 0;

                bw = new BinaryWriter(blfStream);
                bw.Seek(Marshal.SizeOf(typeof(Header)), SeekOrigin.Begin);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            if (bw is null)
                return;

            try
            {
                Compress();

                bw.Flush();
                UInt64 fs = (ulong)bw.BaseStream.Position;
                bw.Seek(0, SeekOrigin.Begin);

                bw.Write(Bytes.ObjectToBytes(
                    new Header()
                    {
                        m_Signature = "LOGG".ToCharArray(),
                        m_StructureSize = (UInt32)Marshal.SizeOf(typeof(Header)),
                        m_CountOfObjects = ObjectsCount,
                        m_TimeStart = new SYSTEMTIME(DatalogTime),
                        m_FileSizeUncompressed = (UInt64)Marshal.SizeOf(typeof(Header)) + ObjectsSize,
                        m_FileSize = fs//(UInt64)Marshal.SizeOf(typeof(Header)) + ObjectsSize,
                    }));
            }
            finally
            {
                bw.Flush();
                bw.Dispose();
                if (fs is not null)
                {
                    fs.Dispose();
                    fs = null;
                }
                bw = null;
            }
        }

        void Compress()
        {
            if (bufferPos == 0)
                return;

            byte[] zipData = null;
            using (MemoryStream memZippedStream = new MemoryStream())
            {
                using (ZlibStream zlStream = new ZlibStream(memZippedStream, CompressionMode.Compress, CompressionLevel.Level4))
                    zlStream.Write(buffer, 0, (int)bufferPos);

                zipData = memZippedStream.ToArray();
            }

            LogContainer log = new LogContainer()
            {
                m_Header = new ObjectHeaderBase()
                {
                    m_Signature = "LOBJ".ToCharArray(),
                    m_ObjectSize = (uint)(Marshal.SizeOf(typeof(LogContainer)) + zipData.Length),
                    m_HeaderSize = (ushort)Marshal.SizeOf(typeof(ObjectHeaderBase)),
                    m_HeaderVersion = 1,
                    m_ObjectType = ObjType.LOG_CONTAINER,
                },
                m_Flags = 2,
                m_SizeUncompressed = bufferPos,
            };

            bw.Write(Bytes.ObjectToBytes(log));
            bw.Write(zipData, 0, zipData.Length);
            bw.Write(padding, 0, zipData.Length & 3);
            bufferPos = 0;
        }

        public void WriteCanMessage(UInt32 CanID, UInt64 Timestamp, byte BusChannel, bool isTx, byte DLC, byte[] CanData)
        {
            if (DLC > 8)
                return;

            CanMessage msg = new CanMessage()
            {
                m_Header = new ObjectHeader()
                {
                    m_Header = new ObjectHeaderBase()
                    {
                        m_Signature = "LOBJ".ToCharArray(),
                        m_HeaderSize = (UInt16)Marshal.SizeOf(typeof(ObjectHeader)),
                        m_HeaderVersion = 1,
                        m_ObjectType = ObjType.CAN_MESSAGE,
                        m_ObjectSize = (UInt32)Marshal.SizeOf(typeof(CanMessage)),
                    },
                    m_Flags = 2,
                    m_TimeStamp = Timestamp * 1000,
                },
                m_Channel = BusChannel,
                m_Flags = (byte)(0 | (isTx.AsByte() & 0x0F)),
                m_DLC = DLC,
                m_ID = CanID,
                m_Data = new byte[8],
            };
            Array.Copy(CanData, msg.m_Data, Math.Min(DLC, msg.m_Data.Length));

            if (bufferPos + msg.m_Header.m_Header.m_ObjectSize >= BufferSize)
                Compress();

            ObjectsCount++;
            ObjectsSize += msg.m_Header.m_Header.m_ObjectSize;

            byte[] data = Bytes.ObjectToBytes(msg);
            Array.Copy(data, 0, buffer, bufferPos, data.Length);
            bufferPos += (uint)data.Length;
        }

        public void WriteCanError(UInt64 Timestamp, byte BusChannel, byte ErrorCode)
        {
            CanError err = new CanError()
            {
                m_Header = new ObjectHeader()
                {
                    m_Header = new ObjectHeaderBase()
                    {
                        m_Signature = "LOBJ".ToCharArray(),
                        m_HeaderSize = (UInt16)Marshal.SizeOf(typeof(ObjectHeader)),
                        m_HeaderVersion = 1,
                        m_ObjectType = ObjType.CAN_ERROR_EXT,
                        m_ObjectSize = (UInt32)Marshal.SizeOf(typeof(CanError)),
                    },
                    m_Flags = 2,
                    m_TimeStamp = Timestamp * 1000,
                },
                m_Channel = BusChannel,
                m_Flags = 2,
                m_ECC = VectorError(ErrorCode),
                m_FlagsExt = VectorErrorExt(ErrorCode),
            };

            if (bufferPos + err.m_Header.m_Header.m_ObjectSize >= BufferSize)
                Compress();

            ObjectsCount++;
            ObjectsSize += err.m_Header.m_Header.m_ObjectSize;

            byte[] data = Bytes.ObjectToBytes(err);
            Array.Copy(data, 0, buffer, bufferPos, data.Length);
            bufferPos += (uint)data.Length;
        }

        public void WriteCanFDMessage(UInt32 CanID, UInt64 Timestamp, byte BusChannel, bool isTx, bool isBRS, byte DLC, byte[] CanData)
        {
            byte canLength = (byte)DlcFDList.IndexOf(DLC);

            CanFDMessage msg = new CanFDMessage()
            {
                m_Header = new ObjectHeader()
                {
                    m_Header = new ObjectHeaderBase()
                    {
                        m_Signature = "LOBJ".ToCharArray(),
                        m_HeaderSize = (UInt16)Marshal.SizeOf(typeof(ObjectHeader)),
                        m_HeaderVersion = 1,
                        m_ObjectType = ObjType.CAN_FD_MESSAGE_64,
                        m_ObjectSize = (UInt32)(Marshal.SizeOf(typeof(CanFDMessage)) + canLength),
                    },
                    m_Flags = 2,
                    m_TimeStamp = Timestamp * 1000,
                },
                m_Channel = BusChannel,
                m_Flags = (uint)((1 << 12) | (isBRS.AsByte() << 13)),
                m_DLC = DLC,
                m_ValidDataBytes = canLength,
                m_Dir = isTx.AsByte(),
                m_ID = CanID,
            };

            if (bufferPos + msg.m_Header.m_Header.m_ObjectSize >= BufferSize)
                Compress();

            ObjectsCount++;
            ObjectsSize += msg.m_Header.m_Header.m_ObjectSize;

            byte[] data = Bytes.ObjectToBytes(msg);
            Array.Copy(data, 0, buffer, bufferPos, data.Length);
            bufferPos += (uint)data.Length;
            Array.Copy(CanData, 0, buffer, bufferPos, canLength);
            bufferPos += canLength;
        }

        public void WriteLinMessage(byte LinID, UInt64 Timestamp, byte BusChannel, bool isTx, byte DLC, byte[] LinData)
        {
            if (DLC > 8)
                return;

            LinMessage2 msg = new LinMessage2()
            {
                m_Header = new ObjectHeader()
                {
                    m_Header = new ObjectHeaderBase()
                    {
                        m_Signature = "LOBJ".ToCharArray(),
                        m_HeaderSize = (UInt16)Marshal.SizeOf(typeof(ObjectHeader)),
                        m_HeaderVersion = 1,
                        m_ObjectType = ObjType.LIN_MESSAGE2,
                        m_ObjectSize = (UInt32)Marshal.SizeOf(typeof(LinMessage2)),
                    },
                    m_Flags = 2,
                    m_TimeStamp = Timestamp * 1000,
                },
                m_LinTimestampEvent = new LinTimestampEvent()
                { 
                    m_LinMsgDescrEvent = new LinMessageDescriptor()
                    {
                        m_LinSynchFieldEvent = new LinSynchFieldEvent()
                        {
                            m_LinBusEvent = new LinBusEvent()
                            {
                                m_SOF = Timestamp * 1000,
                                m_Channel = BusChannel,
                            },
                        },
                        m_DLC = DLC,
                        m_ID = LinID,
                        m_MessageID = LinID,
                    }
                },
                m_Dir = (byte)(0 | (isTx.AsByte() & 0x0F)),
                m_Data = new byte[8],
            };
            Array.Copy(LinData, msg.m_Data, Math.Min(DLC, msg.m_Data.Length));

            if (bufferPos + msg.m_Header.m_Header.m_ObjectSize >= BufferSize)
                Compress();

            ObjectsCount++;
            ObjectsSize += msg.m_Header.m_Header.m_ObjectSize;

            byte[] data = Bytes.ObjectToBytes(msg);
            Array.Copy(data, 0, buffer, bufferPos, data.Length);
            bufferPos += (uint)data.Length;
        }

        public void WriteLinCrcError(byte LinID, UInt64 Timestamp, byte BusChannel, bool isTx, byte DLC, byte[] LinData)
        {
            if (DLC > 8)
                return;

            LinCrcError msg = new LinCrcError()
            {
                m_Header = new ObjectHeader()
                {
                    m_Header = new ObjectHeaderBase()
                    {
                        m_Signature = "LOBJ".ToCharArray(),
                        m_HeaderSize = (UInt16)Marshal.SizeOf(typeof(ObjectHeader)),
                        m_HeaderVersion = 1,
                        m_ObjectType = ObjType.LIN_CRC_ERROR,
                        m_ObjectSize = (UInt32)Marshal.SizeOf(typeof(LinCrcError)),
                    },
                    m_Flags = 2,
                    m_TimeStamp = Timestamp * 1000,
                },
                m_Channel = BusChannel,
                m_DLC = DLC,
                m_Dir = (byte)(0 | (isTx.AsByte() & 0x0F)),
                m_ID = LinID,
                m_Data = new byte[8],
            };
            Array.Copy(LinData, msg.m_Data, Math.Min(DLC, msg.m_Data.Length));

            if (bufferPos + msg.m_Header.m_Header.m_ObjectSize >= BufferSize)
                Compress();

            ObjectsCount++;
            ObjectsSize += msg.m_Header.m_Header.m_ObjectSize;

            byte[] data = Bytes.ObjectToBytes(msg);
            Array.Copy(data, 0, buffer, bufferPos, data.Length);
            bufferPos += (uint)data.Length;
        }

        public void WriteLinSendError(byte LinID, UInt64 Timestamp, byte BusChannel, byte DLC)
        {
            LinSendError msg = new LinSendError()
            {
                m_Header = new ObjectHeader()
                {
                    m_Header = new ObjectHeaderBase()
                    {
                        m_Signature = "LOBJ".ToCharArray(),
                        m_HeaderSize = (UInt16)Marshal.SizeOf(typeof(ObjectHeader)),
                        m_HeaderVersion = 1,
                        m_ObjectType = ObjType.LIN_SND_ERROR,
                        m_ObjectSize = (UInt32)Marshal.SizeOf(typeof(LinSendError)),
                    },
                    m_Flags = 2,
                    m_TimeStamp = Timestamp * 1000,
                },
                m_DLC = DLC,
                m_ID = LinID,
                m_Channel = BusChannel,
            };

            if (bufferPos + msg.m_Header.m_Header.m_ObjectSize >= BufferSize)
                Compress();

            ObjectsCount++;
            ObjectsSize += msg.m_Header.m_Header.m_ObjectSize;

            byte[] data = Bytes.ObjectToBytes(msg);
            Array.Copy(data, 0, buffer, bufferPos, data.Length);
            bufferPos += (uint)data.Length;
        }
    }
}
