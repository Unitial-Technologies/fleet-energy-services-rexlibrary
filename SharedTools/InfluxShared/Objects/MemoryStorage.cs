using InfluxShared.Helpers;
using InfluxShared.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace InfluxShared.Objects
{
    internal sealed class MemoryStorage<T> : MemoryStream, IStorage<T>, IDisposable, IEnumerable<T> where T : struct
    {
        byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
        public int elementSize { get; }
        public long elementCount => Length / elementSize;

        private bool disposedValue;

        public MemoryStorage() : base()
        {
            elementSize = Marshal.SizeOf(typeof(T));
        }

        #region Destructors

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    base.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DoubleData()
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

        public T this[long index]
        {
            get
            {
                if (index < 0 || index > elementCount)
                    throw new ArgumentOutOfRangeException();

                Seek(index * elementSize, SeekOrigin.Begin);
                Read(buffer, 0, (int)elementSize);
                return (T)Convert.ChangeType(buffer, typeof(T));
            }
        }

        public void InitRead()
        {
            Seek(0, SeekOrigin.Begin);
        }

        public void Write(T value) => Write(BitConverter.GetBytes((dynamic)value), 0, elementSize);

        public bool Read(ref T value)
        {
            byte[] tmp = new byte[elementSize];
            if (Read(tmp, 0, elementSize) != elementSize)
                return false;

            value = tmp.ConvertTo<T>();
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            Seek(0, SeekOrigin.Begin);
            for (int index = 0; index < elementCount; index++)
            {
                Read(buffer, 0, (int)elementSize);
                yield return (T)Convert.ChangeType(buffer, typeof(T));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Seek(0, SeekOrigin.Begin);
            for (int index = 0; index < elementCount; index++)
            {
                Read(buffer, 0, (int)elementSize);
                yield return (T)Convert.ChangeType(buffer, typeof(T));
            }
        }

    }
}
