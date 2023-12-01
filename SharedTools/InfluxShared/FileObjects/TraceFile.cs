using System;
using System.IO;
using System.Text;

namespace InfluxShared.FileObjects
{
    public class ASC : TraceFile
    {
        public static readonly string Extension = ".asc";
        public static readonly string Filter = "ASCII Logging File (*.asc)|*.asc";

        public override void WriteHeader(DateTime LogTime)
        {
            traceWriter.WriteLine("date " + LogTime.ToString("ddd MMM dd hh:mm:ss.fff tt yyyy"));
            traceWriter.WriteLine("base hex  timestamps absolute");
            traceWriter.WriteLine("internal events logged");
        }
    }

    public class TRC : TraceFile
    {
        public const string Extension = ".trc";
        public const string Filter = "Peak Can Trace File (*.trc)|*.trc";

        private Int64 rowid = 0;

        public override void WriteHeader(DateTime LogTime)
        {
            traceWriter.WriteLine(";$FILEVERSION=2.1");
            traceWriter.WriteLine(";$STARTTIME=" + LogTime.ToOADate().ToString());
            traceWriter.WriteLine(";$COLUMNS=N,O,T,B,I,d,L,D");
        }

        public override void WriteLine(string traceLine)
        {
            if (traceLine == "")
                return;

            traceWriter.Write((++rowid).ToString().PadLeft(10) + " ");
            base.WriteLine(traceLine);
        }
    }

    public class TraceFile : IDisposable
    {
        internal TextWriter traceWriter = null;
        private bool disposedValue;

        public TraceFile()
        {

        }

        #region Destructors
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ASCII()
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

        public virtual void WriteHeader(DateTime LogTime) { }

        public bool Start(string FileName, DateTime LogTime)
        {
            try
            {
                traceWriter = new StreamWriter(FileName);
                WriteHeader(LogTime);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Start(Stream outStream, DateTime LogTime)
        {
            try
            {
                traceWriter = new StreamWriter(outStream, new UTF8Encoding(false), 1024, true);
                WriteHeader(LogTime);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            if (traceWriter is null)
                return;

            traceWriter.Dispose();
            traceWriter = null;
        }

        public virtual void WriteLine(string traceLine) => traceWriter.WriteLine(traceLine);
    }
}
