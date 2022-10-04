using System;
using System.Runtime.InteropServices;

namespace InfluxShared.Helpers
{
    public static class EnumHelper
    {
        public static T SetFlag<T>(T flags, T flag, bool value)
        {
            UInt64 flagsval = Convert.ToUInt64(flags);
            UInt64 flagval = Convert.ToUInt64(flag);

            if (value)
                flagsval |= flagval;
            else
                flagsval &= ~flagval;

            return (T)Enum.ToObject(typeof(T), flagsval);
        }

        public static T SetFlag<T>(this Enum flags, T flag, bool value)
        {
            UInt64 flagsval = Convert.ToUInt64(flags);
            UInt64 flagval = Convert.ToUInt64(flag);

            if (value)
                flagsval |= flagval;
            else
                flagsval &= ~flagval;

            return (T)Enum.ToObject(typeof(T), flagsval);
        }

        public static byte[] ToBytes(this Enum e)
        {
            byte[] rawdata = new byte[Marshal.SizeOf(e)];
            GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(e, handle.AddrOfPinnedObject(), false);
            handle.Free();

            return rawdata;
        }

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) 
                throw new ArgumentException(string.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
    }
}
