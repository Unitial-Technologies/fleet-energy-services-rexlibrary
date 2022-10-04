using System;
using System.Runtime.InteropServices;

namespace InfluxShared.Generic
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct SYSTEMTIME
    {
        public UInt16 Year;
        public UInt16 Month;
        public UInt16 DayOfWeek;
        public UInt16 Day;
        public UInt16 Hour;
        public UInt16 Minute;
        public UInt16 Second;
        public UInt16 Milliseconds;

        public SYSTEMTIME(DateTime dt)
        {
            //dt = dt.ToUniversalTime();
            Year = (UInt16)dt.Year;
            Month = (UInt16)dt.Month;
            DayOfWeek = (UInt16)dt.DayOfWeek;
            Day = (UInt16)dt.Day;
            Hour = (UInt16)dt.Hour;
            Minute = (UInt16)dt.Minute;
            Second = (UInt16)dt.Second;
            Milliseconds = (UInt16)dt.Millisecond;
        }
    }

}
