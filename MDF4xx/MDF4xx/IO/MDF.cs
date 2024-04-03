using MDF4xx.Blocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MDF4xx.IO
{
    internal class MDF : BlockCollection
    {
        public static readonly string Extension = ".mf4";
        public static readonly string Filter = "ASAM MDF4 file (*.mf4)|*.mf4";
        public static bool UseCompression = true;
        public static UInt64 DefaultDataBlockLength = 1024 * 1024;

        string mdfFileName;
        public string FileName => mdfFileName;

        Int64 mdfFileSize = 0;
        public Int64 FileSize => mdfFileSize;


        public MDF(string path = "") => mdfFileName = path;

        public static MDF Open(string path)
        {
            MDF mdf = new MDF(path);

            using (FileStream stream = new FileStream(mdf.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader br = new BinaryReader(stream))
            {
                mdf.mdfFileSize = stream.Seek(0, SeekOrigin.End);
                stream.Seek(0, SeekOrigin.Begin);

                mdf.id = IDBlock.ReadBlock(br);

                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    Int64 fpos = br.BaseStream.Position;
                    BaseBlock block = BaseBlock.ReadNext(br);
                    block.flink = fpos;
                    mdf.Add(block);

                    if (!mdf.id.Finalized && block is DTBlock)
                        break;
                }
            }

            mdf.Init();

            return mdf;
        }

        public bool Write(Stream stream)
        {
            try
            {
                using (BinaryWriter bw = new BinaryWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), true))
                {
                    bw.Write(id.ToBytes());
                    foreach (KeyValuePair<Int64, BaseBlock> vp in this)
                    {
                        bw.Seek((int)vp.Key, SeekOrigin.Begin);
                        bw.Write(vp.Value.ToBytes());
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
