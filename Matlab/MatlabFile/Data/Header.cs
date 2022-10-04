using InfluxShared.Generic;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace MatlabFile.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class Header
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 116)]
        char[] description;
        public string Description
        {
            get => new string(description).Trim();
            set => description = value.PadRight(116).ToCharArray(0, 116);
        }

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
        byte[] reserved;

        public UInt16 Version;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 2)]
        char[] endianstr;

        public bool IntelByteOrder
        {
            get => new string(endianstr) == "IM";
            set => endianstr = "IM".ToCharArray();
        }

        public Header()
        {
            Description =
                "MATLAB 5.0 MAT-file, " +
                "Platform: " + Environment.OSVersion.ToString() + ", " +
                "Created on: " + DateTime.Now.ToString(CultureInfo.InvariantCulture);
            Version = 0x100;
            IntelByteOrder = true;
        }

        public byte[] ToBytes()
        {
            return Bytes.ObjectToBytes(this);
        }

        internal static Header Read(BinaryReader br)
        {
            Header block = new Header();
            byte[] buffer = br.ReadBytes(Marshal.SizeOf(block));
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Marshal.PtrToStructure(h.AddrOfPinnedObject(), block);
            h.Free();

            return block;
        }
    }
}
