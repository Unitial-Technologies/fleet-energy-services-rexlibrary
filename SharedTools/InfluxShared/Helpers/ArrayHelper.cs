using System;
using System.Collections.Generic;

namespace InfluxShared.Helpers
{
    public static class ArrayHelper
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize));
            using (var enumerator = source.GetEnumerator())
                while (enumerator.MoveNext())
                    yield return YieldBatchElements(enumerator, batchSize - 1);
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (var i = 0; i < batchSize && source.MoveNext(); i++)
                yield return source.Current;
        }

        public static T[] Slice<T>(this T[] source, int index, int length)
        {
            T[] slice = new T[Math.Min(source.Length - index, length)];
            Array.Copy(source, index, slice, 0, slice.Length);
            return slice;
        }

        public static T[][] SliceArray<T>(this T[] source, int chunksize)
        {
            var buffer = new T[(int)Math.Ceiling((double)source.Length / chunksize)][];
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = source.Slice(i * chunksize, chunksize);
            return buffer;
        }

        public static byte[] ExtractBitArray(this byte[] source, int index, int length)
        {
            byte[] bits = new byte[length * 8];
            for (int i = 0; i < bits.Length; i++)
            {
                byte b = source[index + (i >> 3)];
                byte bitmask = (byte)(1 << (i & 7));
                bits[i] = ((b & bitmask) != 0).AsByte();
            }
            return bits;
        }

        public static bool[] ExtractBoolArray(this byte[] source, int index, int length)
        {
            bool[] bits = new bool[length];
            for (int i = 0; i < length; i++)
            {
                byte b = source[index + (i >> 3)];
                byte bitmask = (byte)(1 << (i & 7));
                bits[i] = (b & bitmask) != 0;
            }
            return bits;
        }

        public static void UpdateBits(this byte[] source, int index, byte[] BitArray)
        {
            int bcount = BitArray.Length >> 3;
            for (int i = 0; i < bcount; i++)
            {
                source[index + i] = 0;
                for (int bit = 0; bit < 8; bit++)
                    source[index + i] |= (byte)(BitArray[i * 8 + bit] << bit);
            }
        }

    }
}
