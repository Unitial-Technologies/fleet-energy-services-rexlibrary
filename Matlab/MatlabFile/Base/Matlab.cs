using InfluxShared.FileObjects;
using InfluxShared.Helpers;
using MatlabFile.Data;
using System;
using System.IO;

namespace MatlabFile.Base
{
    public class Matlab : MCollection, IDisposable
    {
        public static readonly string Extension = ".mat";
        public static readonly string Filter = "Matlab 5.0 (*.mat)|*.mat";
        private static readonly string AllowedVariableNameChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        FileStream fs = null;
        BinaryWriter bw = null;
        private bool disposedValue;

        public bool CreateFile(string matFileName)
        {
            fs = new FileStream(matFileName, FileMode.Create);
            return CreateStream(fs);
        }

        public bool CreateStream(Stream matStream)
        {
            try
            {
                bw = new BinaryWriter(matStream);
                bw.Write(header.ToBytes());
                return true;
            }
            catch
            {
                fs = null;
                bw = null;
                return false;
            }
        }

        #region Destructors

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    bw.Flush();
                    if (fs is not null)
                        fs.Dispose();
                    bw.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Matlab()
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

        public bool WriteElement(MElement el, DoubleData data = null)
        {
            try
            {
                el.Write(bw);
                if (data != null)
                    data.Copy(bw.BaseStream, el.DataOffsets);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CreateFromDoubleData(string MatlabFileName, DoubleDataCollection ddata, Action<object> ProgressCallback = null)
        {
            using (var mat = new FileStream(MatlabFileName, FileMode.Create))
                return CreateFromDoubleData(mat, ddata, ProgressCallback);
        }

        public static bool CreateFromDoubleData(Stream MatlabStream, DoubleDataCollection ddata, Action<object> ProgressCallback = null)
        {
            static string PrepareChannelName(string chname)
            {
                string tmp = chname.ReplaceInvalid(AllowedVariableNameChars.ToCharArray(), "_");

                tmp = tmp.Trim("_".ToCharArray());
                while (tmp.IndexOf("__") != -1)
                    tmp = tmp.Replace("__", "_");

                if (tmp.Length == 0 || ((tmp[0] >= '0') && (tmp[0] <= '9')))
                    tmp = "_" + tmp;

                return tmp;
            }

            try
            {
                using (Matlab mat = new Matlab())
                {
                    if (!mat.CreateStream(MatlabStream))
                        throw new Exception("Matlab convertion cannot be initialized!");

                    ProgressCallback?.Invoke(0);
                    ProgressCallback?.Invoke("Writing Matlab data...");
                    for (int i = 0; i < ddata.Count; i++)
                    {
                        DoubleData data = ddata[i];

                        mat.WriteElement(
                            mat.CreateMatrix2D(PrepareChannelName(data.ChannelName), MMatrixType.DoubleArray, 2, data.RecordCount),
                            data
                        );
                        ProgressCallback?.Invoke(i * 100 / ddata.Count);
                    }
                    ProgressCallback?.Invoke(100);

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
