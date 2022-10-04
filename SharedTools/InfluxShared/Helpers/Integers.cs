using InfluxShared.Objects;
using System;
using System.Runtime.InteropServices;

namespace InfluxShared.Helpers
{
    public static class Integers
    {
        public static T ConvertTo<T>(this byte[] obj, int offset = 0)
        {
            GCHandle h = GCHandle.Alloc(obj, GCHandleType.Pinned);
            IntPtr p = h.AddrOfPinnedObject() + offset;
            var output = Marshal.PtrToStructure(p, typeof(T));
            h.Free();
            return (T)output;
        }

        public static dynamic ConvertTo(this byte[] obj, Type TargetType, int offset = 0)
        {
            GCHandle h = GCHandle.Alloc(obj, GCHandleType.Pinned);
            IntPtr p = h.AddrOfPinnedObject() + offset;
            var output = Marshal.PtrToStructure(p, TargetType);
            h.Free();
            return output;
        }

        public static dynamic ReadTo(this byte[] obj, Type TargetType, int TargetElementId = 0)
        {
            GCHandle h = GCHandle.Alloc(obj, GCHandleType.Pinned);
            IntPtr p = h.AddrOfPinnedObject() + TargetElementId * Marshal.SizeOf(TargetType);
            var output = Marshal.PtrToStructure(p, TargetType);
            h.Free();
            return output;
        }

        public static string ToFormatedFileSize(this UInt32 l)
        {
            return string.Format(new FileSizeFormatProvider(), "{0:fs}", l);
        }

        public static byte AsByte(this bool b) => (byte)(b ? 1 : 0);

        public static int Clamp(this int value, int minval, int maxval) => value < minval ? minval : value > maxval ? maxval : value;

    }
}
