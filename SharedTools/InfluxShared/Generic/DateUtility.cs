using System;

namespace InfluxShared.Generic
{
    public static class DateUtility
    {
        public static DateTime FromUnixTimestamp(UInt64 UnixTimestamp) => DateTime.FromOADate(25569 + ((double)UnixTimestamp / 86400));
        public static UInt64 ToUnixTimestamp(DateTime dt) => (UInt64)((double)(dt.ToOADate() - 25569) * 86400);
    }
}
