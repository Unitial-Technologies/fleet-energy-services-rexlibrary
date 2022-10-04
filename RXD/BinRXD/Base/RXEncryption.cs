using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RXD.Base
{
    internal class RXEncryption : IDisposable
    {
        private protected readonly ICryptoTransform Decryptor = null;
        private bool disposedValue;

        internal RXEncryption(byte[] SessionKey)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = SessionKey;
                aes.IV = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                Decryptor = aes.CreateDecryptor();
            }
        }

        #region Destructors
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RXDataReader()
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

        private protected byte[] AesDecrypt(byte[] data)
        {
            byte[] outdata = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, Decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                }
                outdata = ms.ToArray();
            }
            return outdata;
        }

        private protected static byte[] RsaDecrypt(byte[] data)
        {
            try
            {
                RSACryptoServiceProvider rsa;
                if (BinRXD.EncryptionKeysBlob is null)
                {
                    CspParameters par = new CspParameters();
                    par.KeyContainerName = BinRXD.EncryptionContainerName;
                    rsa = new RSACryptoServiceProvider(par);
                }
                else
                {
                    rsa = new();
                    rsa.ImportCspBlob(BinRXD.EncryptionKeysBlob);
                }

                return rsa.Decrypt(data, false);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal static bool DecryptFile(string inputpath, string outputpath)
        {
            try
            {
                using (FileStream fr = new FileStream(inputpath, FileMode.Open))
                using (BinaryReader br = new BinaryReader(fr))
                {
                    byte[] data = br.ReadBytes(RXDataReader.SectorSize);

                    byte[] SessionKey = null;

                    // Parse header block
                    using (MemoryStream mshdr = new MemoryStream(data))
                    using (BinaryReader brhdr = new BinaryReader(mshdr))
                    {
                        UInt16 blockSize = brhdr.ReadUInt16();
                        UInt16 blockVersion = brhdr.ReadUInt16();
                        UInt16 AesKeySize = brhdr.ReadUInt16();
                        UInt16 PubKeySize = brhdr.ReadUInt16();
                        byte[] EncryptedSessionKey = brhdr.ReadBytes(PubKeySize);

                        SessionKey = RsaDecrypt(EncryptedSessionKey);
                        if (SessionKey is null)
                            throw new Exception("Invalid security key!");
                        Array.Resize(ref SessionKey, AesKeySize);
                    }

                    using (RXEncryption rxe = new RXEncryption(SessionKey))
                    using (FileStream fw = new FileStream(outputpath, FileMode.Create))
                    using (BinaryWriter bw = new BinaryWriter(fw))
                    {
                        data = br.ReadBytes(RXDataReader.SectorSize);
                        while (data.Length > 0)
                        {
                            if (!data.SequenceEqual(new byte[RXDataReader.SectorSize]))
                            {
                                data = rxe.AesDecrypt(data);
                                if (data is null)
                                    throw new Exception("Invalid session key!");
                            }
                            bw.Write(data);

                            data = br.ReadBytes(RXDataReader.SectorSize);
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
