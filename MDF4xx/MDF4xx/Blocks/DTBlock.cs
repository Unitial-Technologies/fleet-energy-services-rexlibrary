using MDF4xx.Frames;
using System;
using System.IO;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DTLinks;

namespace MDF4xx.Blocks
{
    internal enum DTLinks
    {
        linkcount
    };

    /// <summary>
    /// Data Block
    /// </summary>
    internal class DTBlock : BaseBlock, IDataBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public byte[] dt_data { get => extraObj; set => extraObj = value; }
        internal override int extraObjSize => (int)DataLength;

        // Data access
        public Int64 DataOffset;
        public Int64 DataLength
        {
            get => (Int64)(header.length - (UInt64)DataOffset);
            set => header.length = (UInt64)(value + DataOffset);
        }

        public UInt64 OrigDatalength => (UInt64)binary.Length;
        public MemoryStream binary;
        public MemoryStream GetStream => binary;

        public void CreateWriteBuffers() => binary = new MemoryStream();

        public void EndWriting()
        {
            binary.Flush();
            DataLength = binary.Length;
        }

        public void FreeWriteBuffers()
        {
            if (binary is not null)
            {
                binary.Dispose();
                binary = null;
            }
        }

        public void WriteFrame(BaseDataFrame frame)
        {
            var fdata = frame.ToBytes();
            binary.Write(fdata, 0, fdata.Length);
        }

        public DTBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;

            DataOffset = Marshal.SizeOf(header) + links.Count * Marshal.SizeOf(typeof(UInt64));
            //DataLength = (Int64)(header.length - (UInt64)DataOffset);
        }
    };
}
