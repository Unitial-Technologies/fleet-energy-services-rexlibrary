using System;
using System.Runtime.InteropServices;

namespace RXD.Blocks
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class BinFooter
    {
        public UInt64 type;
        public UInt16 version;
        public UInt16 length;
        public UInt16 uniqueid;

        public static BinFooter ReadBlock(byte[] bindata)
        {
            BinFooter fs = new BinFooter();
            GCHandle h = GCHandle.Alloc(bindata, GCHandleType.Pinned);
            Marshal.PtrToStructure(h.AddrOfPinnedObject(), fs);
            h.Free();

            return fs;
        }

    }
}
