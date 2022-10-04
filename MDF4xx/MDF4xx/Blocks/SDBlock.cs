using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
	using LinkEnum = SDLinks;
	enum SDLinks
	{
		linkcount
	};

	/// <summary>
	/// Signal Data Block
	/// </summary>
	class SDBlock : BaseBlock
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		internal class BlockData
		{
		}

		/// <summary>
		/// Data block
		/// </summary>
		internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

		public byte[] sd_data { get => extraObj; set => extraObj = value; }
		internal override int extraObjSize => (int)DataLength;

		// Data access
		public Int64 DataOffset;
		public Int64 DataLength
		{
			get => (Int64)(header.length - (UInt64)DataOffset);
			set => header.length = (UInt64)(value + DataOffset);
		}

		public SDBlock(HeaderSection hs = null) : base(hs)
		{
			LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;

			DataOffset = Marshal.SizeOf(header) + links.Count * Marshal.SizeOf(typeof(UInt64));
		}
	};
}
