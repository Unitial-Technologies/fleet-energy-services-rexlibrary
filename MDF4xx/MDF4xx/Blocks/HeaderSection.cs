using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    /// <summary>
    /// Header structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    internal class HeaderSection
    {
        /// <summary>
        /// Block type identifier - "##" + "id"
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
        char[] id;

        /// <summary>
        /// Reserved used for 8-Byte alignment
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
        byte[] reserved;

        /// <summary>
        /// Length of block
        /// </summary>
        public UInt64 length;

        /// <summary>
        /// Number of links
        /// </summary>
        public UInt64 link_count;

        public BlockType Type
        {
            get
            {
                if (Enum.TryParse(new string(id).TrimStart('#'), out BlockType bt))
                    return bt;
                else
                    return BlockType.Unknown;
            }
            set
            {
                if (value == BlockType.Unknown)
                    id = "".PadRight(4).ToCharArray();
                else
                    id = ("##" + value.ToString()).ToCharArray();
            }
        }

        public HeaderSection()
        {
            reserved = new byte[4];
        }

        public static HeaderSection ReadBlock(BinaryReader br)
        {
            HeaderSection hs = new HeaderSection();
            byte[] buffer = br.ReadBytes(Marshal.SizeOf(hs));
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.PtrToStructure(h.AddrOfPinnedObject(), hs);
            h.Free();

            return hs;
        }
    };
}
