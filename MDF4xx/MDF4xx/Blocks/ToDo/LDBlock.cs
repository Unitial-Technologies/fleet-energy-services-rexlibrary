using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.LDLinks;

namespace MDF4xx.Blocks
{
    internal enum LDLinks
    {
        linkcount
    };

    /// <summary>
    /// List Data Block
    /// </summary>
    internal class LDBlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public LDBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            //data = new BlockData();
        }
    };
}
