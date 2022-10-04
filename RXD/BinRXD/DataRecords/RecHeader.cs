using System;
using System.Runtime.InteropServices;

namespace RXD.DataRecords
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    class RecHeader
    {
        public UInt16 UID;
        public byte InfSize;
        public byte DLC;
    }
}
