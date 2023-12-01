using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DLLinks;

namespace MDF4xx.Blocks
{
    internal enum DLLinks
    {
        linkcount
    };

    /// <summary>
    /// Data List Block
    /// </summary>
    internal class DLBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public DLBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            //data = new BlockData();
        }
    };
}
