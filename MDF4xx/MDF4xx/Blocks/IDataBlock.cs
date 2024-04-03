using MDF4xx.Frames;
using System;
using System.IO;


namespace MDF4xx.Blocks
{
    internal interface IDataBlock
    {
        public MemoryStream GetStream { get; }
        public UInt64 OrigDatalength { get; }

        public void CreateWriteBuffers();
        public void WriteFrame(BaseDataFrame frame);
        public void EndWriting();
        public void FreeWriteBuffers();
    }
}
