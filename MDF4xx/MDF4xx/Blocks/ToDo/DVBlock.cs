using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DVLinks;

namespace MDF4xx.Blocks
{
    internal enum DVLinks
    {
        linkcount
    };

    /// <summary>
    /// Data Values Block
    /// </summary>
    internal class DVBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public DVBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            //data = new BlockData();
        }
    };
}
