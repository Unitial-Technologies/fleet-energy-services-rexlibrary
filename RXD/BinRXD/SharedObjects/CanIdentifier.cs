using System;
using System.Runtime.InteropServices;

namespace SharedObjects
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct CanIdentifier
    {
        public static byte AllAddress = 0xFF;
        public static byte NullAddress = 0xFE;
        static UInt32 pgnPdu2Flag = 0xF000u;
        static UInt32 identPdu2Flag = pgnPdu2Flag << 8;

        public UInt32 RawIdent { get; set; }

        // 3 bits unused
        public uint Unused
        {
            get => (RawIdent >> 29) & 0x7u;
            set => RawIdent = (RawIdent & ~(0x7u << 29)) | (value & 0x7u) << 29;
        }

        // 3bit - 0x0..0x7 - Priority
        public uint Priority
        {
            get => (RawIdent >> 26) & 0x7u;
            set => RawIdent = (RawIdent & ~(0x7u << 26)) | (value & 0x7u) << 26;
        }

        public UInt32 ExtendedDataPage
        {
            get => (RawIdent >> 25) & 0x1u;
            set => RawIdent = (RawIdent & ~(0x1u << 25)) | (value & 0x1u) << 25;
        }

        public UInt32 DataPage
        {
            get => (RawIdent >> 24) & 0x1u;
            set => RawIdent = (RawIdent & ~(0x1u << 24)) | (value & 0x1u) << 24;
        }

        /// <summary>
        /// 17bit (0x00000..0x1ffff) Including: DataPage (1bit), PGN - 0xFFFF (16bits)
        /// If PGN is lower than 0xF000 then applied mask is 0xFF00. Second byte means destination address
        /// Otherwise PGN is masked by 0xFFFF. Second byte means group extension and is part of PGN.
        /// </summary>
        public UInt32 PGN
        {
            get =>
                ((RawIdent & identPdu2Flag) == identPdu2Flag) ?
                (RawIdent >> 8) & 0x1FFFFu :
                (RawIdent >> 8) & 0x1FF00u;
            set => RawIdent =
                ((value & pgnPdu2Flag) == pgnPdu2Flag) ?
                (RawIdent & ~(0x1FFFFu << 8)) | (value & 0x1FFFFu) << 8 :
                (RawIdent & ~(0x1FF00u << 8)) | (value & 0x1FF00u) << 8;
        }

        // If PGNOnly < 240 - PGNOnly is destination
        public uint Destination
        {
            get =>
                ((RawIdent & identPdu2Flag) == identPdu2Flag) ?
                NullAddress :
                (RawIdent >> 8) & 0xFFu;
            set
            {
                if ((RawIdent & identPdu2Flag) != identPdu2Flag)
                    RawIdent = (RawIdent & ~(0xFFu << 8)) | (value & 0xFFu) << 8;
            }
        }

        // 8bit - 0x00..0xFF - Source
        public uint Source
        {
            get => RawIdent & 0xFFu;
            set => RawIdent = (RawIdent & ~0xFFu) | (value & 0xFFu);
        }

        public bool ValidSource => Source != AllAddress && Source != NullAddress;
        /*public static bool operator ==(CanIdentifier id1, CanIdentifier id2) => id1.PGN == id2.PGN;
        public static bool operator !=(CanIdentifier id1, CanIdentifier id2) => !(id1 == id2);
        public static bool operator ==(CanIdentifier id1, UInt32 id2) => id1 == new CanIdentifier(id2);
        public static bool operator !=(CanIdentifier id1, UInt32 id2) => !(id1 == id2);
        public static bool IsSame(UInt32 id1, UInt32 id2) => new CanIdentifier(id1) == new CanIdentifier(id2);*/
        public static bool IsPassFilter(CanIdentifier mask, CanIdentifier id) => mask.Source == NullAddress ? id.ValidSource && mask.PGN == id.PGN : mask.Source == id.Source && mask.PGN == id.PGN;

        public static implicit operator UInt32(CanIdentifier id) => id.RawIdent;
        public static implicit operator CanIdentifier(UInt32 id) => new CanIdentifier(id);

        public CanIdentifier(UInt32 CanIdent) => RawIdent = CanIdent;
    }
}
