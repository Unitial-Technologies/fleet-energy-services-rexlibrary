using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.RVLinks;

namespace MDF4xx.Blocks
{
    internal enum RVLinks
    {
        linkcount
    };

    /// <summary>
    /// Reduction Values Block
    /// </summary>
    internal class RVBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public RVBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            //data = new BlockData();
        }
    };
}
