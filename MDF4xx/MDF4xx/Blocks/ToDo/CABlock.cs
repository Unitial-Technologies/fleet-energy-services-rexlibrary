using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.CALinks;

namespace MDF4xx.Blocks
{
    internal enum CALinks
    {
        linkcount
    };

    /// <summary>
    /// Channel Array Block
    /// </summary>
    internal class CABlock : BaseBlock
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        public CABlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            //data = new BlockData();
        }
    };
}
