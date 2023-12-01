using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DILinks;

namespace MDF4xx.Blocks
{
    internal enum DILinks
    {
        linkcount
    };

    /// <summary>
    /// Invalidation Data Block
    /// </summary>
    internal class DIBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        /// <summary>
        /// Length of the data section must be a multiple of cg_inval_bytes.
        /// </summary>
        public byte[] di_data { get => extraObj; set => extraObj = value; }

        public DIBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
        }
    };
}
