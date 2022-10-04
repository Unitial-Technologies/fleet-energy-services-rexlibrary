using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace InfluxShared.Objects
{
    public class TripleDESFileStream : IDisposable
    {
        protected readonly string FileName;

        protected readonly TripleDES CryptObj = null;
        protected readonly CryptoStreamMode mode;
        protected readonly FileStream fstream = null;
        protected readonly CryptoStream cstream = null;
        private bool disposedValue;

        public TripleDESFileStream(string LogFileName, CryptoStreamMode mode, TripleDES CryptObj)
        {
            FileName = LogFileName;
            this.CryptObj = CryptObj;
            this.mode = mode;

            if (mode == CryptoStreamMode.Read)
            {
                fstream = new FileStream(LogFileName, FileMode.Open, FileAccess.Read);
                cstream = new CryptoStream(fstream, CryptObj.CreateDecryptor(), CryptoStreamMode.Read);
            }
            else if (mode == CryptoStreamMode.Write)
            {
                fstream = new FileStream(LogFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                
                byte[] previous = null;
                int previousLength = 0;

                long length = fstream.Length;

                if (length != 0)
                {
                    byte[] block = new byte[CryptObj.IV.Length];

                    if (length >= CryptObj.IV.Length * 2)
                    {
                        fstream.Position = length - CryptObj.IV.Length * 2;
                        fstream.Read(block, 0, block.Length);
                        CryptObj.IV = block;
                    }
                    else
                    {
                        fstream.Position = length - CryptObj.IV.Length;
                    }

                    fstream.Read(block, 0, block.Length);
                    fstream.Position = length - CryptObj.IV.Length;

                    using (var ms = new MemoryStream(block))
                    using (ICryptoTransform decryptor = CryptObj.CreateDecryptor())
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        previous = new byte[CryptObj.IV.Length];
                        previousLength = cs.Read(previous, 0, previous.Length);
                    }
                }

                cstream = new CryptoStream(fstream, CryptObj.CreateEncryptor(), CryptoStreamMode.Write);
                if (previousLength > 0)
                    cstream.Write(previous, 0, previousLength);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    BeforeDispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void BeforeDispose()
        {
            Flush();
            if (mode == CryptoStreamMode.Write)
            {
                cstream?.FlushFinalBlock();
            }
            cstream.Close();
            fstream.Close();
            cstream?.Dispose();
            fstream?.Dispose();
        }

        public void Flush()
        {
            if (mode != CryptoStreamMode.Write)
                return;

            cstream.Flush();
            fstream.Flush();
        }

        public void Write(string message)
        {
            if (mode != CryptoStreamMode.Write)
                return;

            byte[] data = Encoding.UTF8.GetBytes(message);
            if (cstream.CanWrite)
                cstream.Write(data, 0, data.Length);
        }

        public string Read()
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                cstream.CopyTo(memstream);
                memstream.Seek(0, SeekOrigin.Begin);
                return new StreamReader(memstream).ReadToEnd();
            }             
        }
    }
}
