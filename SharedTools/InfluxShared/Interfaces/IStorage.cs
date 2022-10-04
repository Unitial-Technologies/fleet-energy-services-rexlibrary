using System;
using System.Collections.Generic;

namespace InfluxShared.Interfaces
{
    internal interface IStorage<T> : IDisposable, IEnumerable<T> where T : struct
    {
        int elementSize { get; }
        long elementCount { get; }

        long Length { get; }
        long Position { get; set; }

        public T this[long index] { get; }

        public void InitRead();

        public void Write(T value);

        public bool Read(ref T value);

    }
}
