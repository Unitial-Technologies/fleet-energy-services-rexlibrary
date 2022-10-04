using InfluxShared.Generic;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
	[Flags]
	enum FinalizationFlags
	{
		/// <summary>
		/// </summary>

	}

	/// <summary>
	/// File Identification Block
	/// </summary>
	class IDBlock
	{
		public static string id_finalized = "MDF     ";
		public static string id_unfinalized = "UnFinMF ";
		public static UInt16 lastversion = 411;

		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		internal class BlockData
		{

			/// <summary>
			/// File identifier, always contains "MDF     " ("MDF" followed by five spaces, no zero termination), 
			/// except for "unfinalized" MDF files (see 5.5.2 Unfinalized MDF).The file identifier for unfinalized
			/// MDF files contains "UnFinMF " ("UnFinMF" followed by one space, no zero termination).
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
			char[] id_file;

			/// <summary>
			/// Format identifier, a textual representation of the format version for display, e.g. "4.11" (including
			/// zero termination) or "4.11    " (followed by spaces, no zero termination required if 4 spaces).
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
			char[] id_vers;

			/// <summary>
			/// Program identifier, to identify the program which generated the MDF file(no zero termination required).
			/// This program identifier serves only for compatibility with previous MDF format versions.Detailed
			/// information about the generating application must be written to the first FHBLOCK referenced by the HDBLOCK.
			/// As a recommendation, the program identifier inserted into the 8 characters should be the base
			/// name(first 8 characters) of the EXE / DLL of the writing application.Alternatively, also version
			/// information of the application can be appended (e.g. "MyApp45" for version 4.5 of MyApp.exe).
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
			char[] id_prog;

			/// <summary>
			/// Reserved (must be 0 for compatibility reasons!)
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 4)]
			byte[] id_reserved1;

			/// <summary>
			/// Version number of the MDF format, i.e. 411 for this version
			/// </summary>
			UInt16 id_ver;

			/// <summary>
			/// Reserved
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 30)]
			byte[] id_reserved2;

			/// <summary>
			/// Standard flags for unfinalized MDF
			/// Bit combination of flags that indicate the steps required to finalize the MDF file.For a finalized
			/// MDF file, the value must be 0 (no flag set).See also the description in section 5.5.2 Unfinalized
			/// MDF and for id_custom_unfin_flags. The bit flags for id_unfin_flags are defined below in Table 13.
			/// Note that for the currently defined standard flags, the respective finalization steps must be executed
			/// in the following order(given that the respective bit is set in id_unfin_flags) :
			/// 1. Update DL block(bit 4)
			/// 2. Update DT / RD block(bit 2 and bit 3)
			/// 3. All other standard steps
			/// Custom finalization flags(see id_custom_unfin_flags) also may require a specific order which may
			/// be intertwined with the above ordering.
			/// </summary>
			public UInt16 id_unfin_flags;

			/// <summary>
			/// Custom flags for unfinalized MDF
			/// Bit combination of flags that indicate custom steps required to finalize the MDF file.For a finalized
			/// MDF file, the value must be 0 (no flag set).See also 5.5.2 Unfinalized MDF.
			/// Custom flags should only be used to handle cases that are not covered by the(currently known)
			/// standard flags in id_unfin_flags.The meaning of the flags depends on the creator tool, i.e.the
			/// application that has written the MDF file or executed the most recent modification(see id_prog
			/// and last entry in file history).No tool should modify the MDF file(and thus write a new entry to the
			/// file history) before it was finalized correctly at least for all custom finalization steps.
			/// Finalization should only be done by the creator tool or a tool that is familiar with all custom
			/// finalization steps required for a file from this creator tool.
			/// </summary>
			public UInt16 id_custom_unfin_flags;

			internal bool Finalized
			{
				get
				{
					return new string(id_file) == id_finalized;
				}
				set
				{
					if (value)
					{
						id_file = id_finalized.ToCharArray();
						id_unfin_flags = 0;
					}
					else
					{
						id_file = id_unfinalized.ToCharArray();
						id_unfin_flags = 0x7f; // Full bitmask set
					}
				}
			}

			internal UInt16 Version
			{
				get
				{
					return id_ver;
				}
				set
				{
					id_ver = value;
					id_vers = ((value / 100).ToString() + "." + (value % 100).ToString()).PadRight(8, ' ').ToCharArray();
				}
			}

			internal BlockData()
			{
				id_prog = "".PadRight(8, ' ').ToCharArray();
				id_reserved1 = new byte[4];
				id_reserved2 = new byte[30];
			}
		}

		/// <summary>
		/// Data block
		/// </summary>
		internal BlockData data;

		public UInt64 Size => (UInt64)Marshal.SizeOf(data);

		public bool Finalized { get => data.Finalized; set => data.Finalized = value; }

		public UInt16 Version { get => data.Version; set => data.Version = value; }

		//public bool GetFlag(CNFlags flag) => data.id_unfin_flags.HasFlag(flag);
		//public void SetFlag(CNFlags flag, bool value) => data.id_unfin_flags = Generic.SetFlag(data.id_unfin_flags, flag, value);

		// ID Flags
		public bool FlagCACGCycleCounters => (data.id_unfin_flags & 0x0001) != 0;
		public bool FlagSRCycleCounters => (data.id_unfin_flags & 0x0002) != 0;
		public bool FlagDTLastLength => (data.id_unfin_flags & 0x0004) != 0;
		public bool FlagRDLastLength => (data.id_unfin_flags & 0x0008) != 0;
		public bool FlagDLChainedLast => (data.id_unfin_flags & 0x0010) != 0;
		public bool FlagVlsdSize => (data.id_unfin_flags & 0x0020) != 0;
		public bool FlagVlsdOffsetValues => (data.id_unfin_flags & 0x0040) != 0;

		public IDBlock()
		{
			data = new BlockData();
			Finalized = true;
			Version = lastversion;
		}

		public byte[] ToBytes()
		{
			return Bytes.ObjectToBytes(data);
		}

		public static IDBlock ReadBlock(BinaryReader br)
		{
			IDBlock block = new IDBlock();
			byte[] buffer = br.ReadBytes(Marshal.SizeOf(block.data));
			GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			Marshal.PtrToStructure(h.AddrOfPinnedObject(), block.data);
			h.Free();

			return block;
		}

		public IDBlock Clone()
		{
			byte[] data = ToBytes();
			MemoryStream ms = new MemoryStream(data);
			using (BinaryReader br = new BinaryReader(ms))
			{
				return ReadBlock(br);
			}
		}

	};
}
