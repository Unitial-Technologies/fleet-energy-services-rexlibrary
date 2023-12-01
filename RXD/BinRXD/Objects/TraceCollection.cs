using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace RXD.Objects
{
    public class TraceCollection : List<TraceRow>
    {
        public DateTime StartLogTime = DateTime.Now;

        public TraceCollection()
        {

        }

        public string asASCII
        {
            get
            {
                string ascii = "";
                foreach (var rec in this)
                    ascii += rec.asASCII;
                return ascii;
            }
        }

        public bool ToASCII(string FileName, Action<object> ProgressCallback)
        {
            try
            {
                using (ASC asc = new ASC())
                {
                    if (asc.Start(FileName, StartLogTime))
                    {
                        ProgressCallback?.Invoke(0);
                        ProgressCallback?.Invoke("Writing ASCII file...");
                        for (int i = 0; i < Count; i++)
                        {
                            asc.WriteLine(this[i].asASCII);
                            ProgressCallback?.Invoke(i * 100 / Count);
                        }
                        ProgressCallback?.Invoke(100);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool ToASCII(Stream ascStream, Action<object> ProgressCallback)
        {
            try
            {
                using (ASC asc = new ASC())
                {
                    if (asc.Start(ascStream, StartLogTime))
                    {
                        ProgressCallback?.Invoke(0);
                        ProgressCallback?.Invoke("Writing ASCII stream...");
                        for (int i = 0; i < Count; i++)
                        {
                            asc.WriteLine(this[i].asASCII);
                            ProgressCallback?.Invoke(i * 100 / Count);
                        }
                        ProgressCallback?.Invoke(100);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public string asTRC
        {
            get
            {
                string trc = "";
                foreach (var rec in this)
                    trc += rec.asTRC;
                return trc;
            }
        }

        public bool ToTRC(string FileName, Action<object> ProgressCallback)
        {
            try
            {
                using (TRC trc = new TRC())
                {
                    if (trc.Start(FileName, StartLogTime))
                    {
                        ProgressCallback?.Invoke(0);
                        ProgressCallback?.Invoke("Writing TRC file...");
                        for (int i = 0; i < Count; i++)
                        {
                            trc.WriteLine(this[i].asTRC);
                            ProgressCallback?.Invoke(i * 100 / Count);
                        }
                        ProgressCallback?.Invoke(100);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool ToTRC(Stream traceStream, Action<object> ProgressCallback)
        {
            try
            {
                using (TRC trc = new TRC())
                {
                    if (trc.Start(traceStream, StartLogTime))
                    {
                        ProgressCallback?.Invoke(0);
                        ProgressCallback?.Invoke("Writing TRC stream...");
                        for (int i = 0; i < Count; i++)
                        {
                            trc.WriteLine(this[i].asTRC);
                            ProgressCallback?.Invoke(i * 100 / Count);
                        }
                        ProgressCallback?.Invoke(100);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
