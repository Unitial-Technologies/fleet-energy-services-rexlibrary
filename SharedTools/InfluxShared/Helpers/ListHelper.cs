using System;
using System.Collections.Generic;

namespace InfluxShared.Helpers
{
    public static class ListHelper
    {
        public static int FindFirstIndexGreaterThanOrEqualTo<T>(this IList<T> sortedCollection, T key) where T : IComparable<T>
        {
            int begin = 0;
            int end = sortedCollection.Count;
            while (end > begin)
            {
                int index = (begin + end) / 2;
                T el = sortedCollection[index];
                if (el.CompareTo(key) >= 0)
                    end = index;
                else
                    begin = index + 1;
            }
            return end;
        }

        public static T[] ToArray<T>(this SortedList<T, T> collection)
        {
            T[] arr = new T[collection.Count * 2];
            for (int i = 0; i < collection.Count; i++)
            {
                arr[i * 2] = collection.Keys[i];
                arr[i * 2 + 1] = collection.Values[i];
            }
            return arr;
        }

        public static double[] ApplyFxToArray(this SortedList<double, double> collection, double Factor, double Offset)
        {
            double[] arr = new double[collection.Count * 2];
            for (int i = 0; i < collection.Count; i++)
            {
                arr[i * 2] = (collection.Keys[i] - Offset) / Factor;
                arr[i * 2 + 1] = collection.Values[i];
            }
            return arr;
        }

    }
}
