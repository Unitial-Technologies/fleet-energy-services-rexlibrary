using System;
using System.Runtime.InteropServices;

namespace InfluxShared.Generic
{
    public class PinObj : IDisposable
    {
        GCHandle handle;
        bool disposed = false;

        public PinObj(object obj)
        {
            handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        public static implicit operator IntPtr(PinObj obj) => obj.handle.AddrOfPinnedObject();

        public static implicit operator UInt32(PinObj obj) => (UInt32)Marshal.ReadInt32(obj.handle.AddrOfPinnedObject());

        public static implicit operator UInt64(PinObj obj) => (UInt64)Marshal.ReadInt64(obj.handle.AddrOfPinnedObject());

        public static implicit operator byte[](PinObj obj) => (byte[])obj.handle.Target;

        public static implicit operator string(PinObj obj) => Marshal.PtrToStringAnsi(obj.handle.AddrOfPinnedObject());

        public T Object<T>() => (T)handle.Target;

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose()
        {
            if (disposed)
                return;

            handle.Free();
            disposed = true;
        }

        ~PinObj()
        {
            DoDispose();
        }

    }
}
