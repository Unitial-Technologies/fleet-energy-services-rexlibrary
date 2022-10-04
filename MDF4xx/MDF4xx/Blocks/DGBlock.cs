using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
	using LinkEnum = DGLinks;
	enum DGLinks
	{
		/// <summary>
		/// Pointer to next data group block (DGBLOCK) (can be NIL)
		/// </summary>
		dg_dg_next,
		/// <summary>
		/// Pointer to first channel group block (CGBLOCK) (can be NIL)
		/// </summary>
		dg_cg_first,
		/// <summary>
		/// Pointer to data block (DT-/DVBLOCK or DZBLOCK for this block type) or data list block (DL-/LDBLOCK or HLBLOCK if required) (can be NIL) 
		/// Note: if the child CGBLOCK references a remote master group (cg_cg_master), then only DV- or LDBLOCK is allowed.
		/// </summary>
		dg_data,
		/// <summary>
		/// Pointer to comment and additional information (TXBLOCK or MDBLOCK) (can be NIL)
		/// </summary>
		dg_md_comment,
		linkcount
	}

	/// <summary>
	/// Data Group Block
	/// </summary>
	class DGBlock : BaseBlock
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		internal class BlockData
		{
			/// <summary>
			/// Number of Bytes used for record IDs in the data block.
			/// 0 = data records without record ID(only possible for sorted data group) 
			/// 1 = record ID(UINT8) before each data record 
			/// 2 = record ID(UINT16, LE Byte order) before each data record 
			/// 4 = record ID(UINT32, LE Byte order) before each data record 
			/// 8 = record ID(UINT64, LE Byte order) before each data record
			/// Must be 0 in case dg_data references a LDBLOCK or a DVBLOCK.
			/// </summary>
			public byte dg_rec_id_size;

			/// <summary>
			/// Reserved
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 7)]
			byte[] dg_reserved;
		}

		/// <summary>
		/// Data block
		/// </summary>
		internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

		// Objects to direct access childs
		public DGBlock dg_next => links.GetObject(LinkEnum.dg_dg_next);
		public CGBlock cg_first => links.GetObject(LinkEnum.dg_cg_first);
		public BaseBlock dg_data => links.GetObject(LinkEnum.dg_data);
		public MDBlock md_comment => links.GetObject(LinkEnum.dg_md_comment);

		public DGBlock(HeaderSection hs = null) : base(hs)
		{
			LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
			data = new BlockData();
		}

		public List<CGBlock> GetGroupList()
		{
			List<CGBlock> cglist = new List<CGBlock>();
			CGBlock cg = cg_first;
			while (cg != null)
			{
				cglist.Add(cg);
				cg = cg.cg_next;
			}

			return cglist;
		}

		public Dictionary<UInt64, CGBlock> GetGroupDict(bool resetcycles = false, bool resetvlsdsize = false)
		{
			Dictionary<UInt64, CGBlock> cglist = new Dictionary<UInt64, CGBlock>();
			CGBlock cg = cg_first;
			while (cg != null)
			{
				if (resetcycles)
					cg.data.cg_cycle_count = 0;
				if (resetvlsdsize)
					cg.data.cg_size.vlsd_size = 0;
				cglist.Add(cg.data.cg_record_id, cg);
				cg = cg.cg_next;
			}

			return cglist;
		}

		public void AppendCG(CGBlock newcg)
		{
			if (cg_first is null)
			{
				links.SetObject(LinkEnum.dg_cg_first, newcg);
			}
			else
			{
				CGBlock cg = cg_first;
				while (cg.cg_next != null)
					cg = cg.cg_next;

				cg.links.SetObject(CGLinks.cg_cg_next, newcg);
			}
		}

	};
}
