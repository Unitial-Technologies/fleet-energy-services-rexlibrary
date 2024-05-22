using System;
using System.Collections.Generic;

namespace RXD.Base
{
    public delegate BinRXD FileLoaderFunc(string fn);

    public class RXDLogger : IDisposable
    {
        public string id;
        List<string> FileList = null;
        public string FileName = "";

        public BinRXD rxd = null;
        internal RXDataSyncReader dr = null;
        internal byte CanBusCount = 0;

        private bool disposedValue;

        public FileLoaderFunc LoadFileMethod;

        public RXDLogger(string id, List<string> FileList, FileLoaderFunc rxdLoader = null)
        {
            this.id = id;
            this.FileList = FileList;
            this.FileList.Sort();

            LoadFileMethod = rxdLoader is null ? BinRXD.Load : rxdLoader;
            LoadNext(out rxd);
            if (rxd is not null)
                CanBusCount = (byte)(rxd.GetLastBusID + 1);
        }

        string NextFileName()
        {
            if (FileList is null)
                return string.Empty;

            if (FileList.Count == 0)
                return string.Empty;

            FileName = FileList[0];
            FileList.RemoveAt(0);
            return FileName;
        }

        bool LoadNext(out BinRXD rxd)
        {
            string fn = NextFileName();
            if (fn == string.Empty)
            {
                rxd = null;
                return false;
            }

            rxd = LoadFileMethod(fn);
            return true;
        }

        public bool LoadNextFile(bool isFirst = false)
        {
            if (rxd is not null || isFirst)
            {
                while (!LoadNext(out rxd)) 
                    if (FileList.Count ==0)
                        return false;

                if (rxd is not null)
                {
                    dr = new RXDataSyncReader(rxd);
                    return true;
                }
            }

            dr = null;
            return false;
        }

        
        #region Destructors
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (dr != null) dr.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RXDLogger()
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
    }
}
