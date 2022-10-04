using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    using LinkEnum = RILinks;
    enum RILinks
    {
        linkcount
    };

    /// <summary>
    /// Reduction Data Invalidation Block
    /// </summary>
    class RIBlock : BaseBlock
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
