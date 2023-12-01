using System;
using System.Runtime.InteropServices;
using System.Text;
using LinkEnum = MDF4xx.Blocks.TXLinks;

namespace MDF4xx.Blocks
{
    internal enum TXLinks
    {
        linkcount
    }

    /// <summary>
    /// Text Block
    /// </summary>
    internal class TXBlock : BaseBlock
    {
        /// <summary>
        /// Plain text string UTF-8 encoded, zero terminated, new line indicated by CR and LF.
        /// </summary>
        public string tx_data
        {
            get => Encoding.UTF8.GetString(extraObj);
            set
            {
                extraObj = new byte[(value.Length + 8) & ~7];
                GCHandle h = GCHandle.Alloc(extraObj, GCHandleType.Pinned);
                IntPtr p = h.AddrOfPinnedObject();
                Marshal.Copy(Encoding.UTF8.GetBytes(value), 0, p, value.Length);
                h.Free();
            }
        }

        public TXBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
        }
    };
}
