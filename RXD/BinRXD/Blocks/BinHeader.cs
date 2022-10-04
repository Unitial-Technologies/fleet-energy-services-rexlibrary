using System;
using System.Runtime.InteropServices;

namespace RXD.Blocks
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class BinHeader
    {
        public BlockType type;
        public UInt16 version;
        public UInt16 length;
        public UInt16 uniqueid;

        public static BinHeader ReadBlock(byte[] bindata)
        {
            BinHeader hs = new BinHeader();
            GCHandle h = GCHandle.Alloc(bindata, GCHandleType.Pinned);
            Marshal.PtrToStructure(h.AddrOfPinnedObject(), hs);
            h.Free();

            return hs;
        }

    }
}
