using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.RILinks;

namespace MDF4xx.Blocks
{
    internal enum RILinks
    {
        linkcount
    };

    /// <summary>
    /// Reduction Data Invalidation Block
    /// </summary>
    internal class RIBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public RIBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            //data = new BlockData();
        }
    };
}
