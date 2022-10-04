using System;
using System.Collections.Generic;
using System.Linq;

namespace InfluxShared.Helpers
{
    public static class LinqHelper
    {
        public static IEnumerable<T> TakeLastElements<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
