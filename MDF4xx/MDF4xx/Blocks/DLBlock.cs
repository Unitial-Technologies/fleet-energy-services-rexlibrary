using InfluxShared.Generic;
using InfluxShared.Helpers;
using MDF4xx.IO;
using System;
using System.Runtime.InteropServices;
using LinkEnum = MDF4xx.Blocks.DLLinks;

namespace MDF4xx.Blocks
{
    internal enum DLLinks
    {
        /// <summary>
        /// Pointer to next data list block (DLBLOCK) (can be NIL).
        /// </summary>
        dl_dl_next,
        linkcount
    };

    [Flags]
    internal enum DLFlags : byte
    {
        /// <summary>
        /// No flag is set
        /// </summary>
        None = 0,
        /// <summary>
        /// Bit 0
        /// Equal length flag.
        /// If set, each DLBLOCK in the linked list has the same number of referenced blocks (dl_count) and the (uncompressed) data sections of the blocks referenced by dl_data have a common length given by dl_equal_length.
        /// The only exception is that for the last DLBLOCK in the list (dl_dl_next = NIL), its number of referenced blocks dl_count can be less than or equal to dl_count of the previous DLBLOCK, and the data section length of its last referenced block (dl_data[dl_count-1]) can be less than or equal to dl_equal_length.
        /// If not set, the number of referenced blocks dl_count may be different for each DLBLOCK in the linked list, and the data section lengths of the referenced blocks may be different and a table of offsets is given in dl_offset.
        /// Note: The value of the "equal length" flag must be equal for all DLBLOCKs in the linked list.
        /// </summary>
        EqualLength = 1 << 0,
        /// <summary>
        /// Time values flag.
        /// If set, the DLBLOCK contains a list of (raw) time values in dl_time_values which can be used to improve performance for binary search of time values in the list of referenced data blocks.
        /// The bit must not be set if the DLBLOCK references signal data blocks or if the DLBLOCK contains records from multiple channel groups (i.e. if parent is an unsorted data group). The bit can only be set if the channel group which defines the record layout contains either a virtual or a non-virtual time master channel (cn_type = 2 or 3 and cn_sync_type = 1). Note that for RDBLOCKs, it only makes sense to store the time values if the parent SRBLOCK has sr_sync_type = 1(time).
        /// Note: The value of the "time values" flag must be equal for all DLBLOCKs in the linked list. Valid since MDF 4.2.0, should not be set for earlier versions
        /// </summary>
        TimeValues = 1 << 1,
        /// <summary>
        /// Angle values flag.
        /// If set, the DLBLOCK contains a list of (raw) angle values in dl_angle_values which can be used to improve performance for binary search of angle values in the list of referenced data blocks.
        /// The bit must not be set if the DLBLOCK references signal data blocks or if the DLBLOCK contains records from multiple channel groups (i.e. if parent is an unsorted data group). The bit can only be set if the channel group which defines the record layout contains either a virtual or a non-virtual angle master channel (cn_type = 2 or 3 and cn_sync_type = 2). Note that for RDBLOCKs, it only makes sense to store the angle values if the parent SRBLOCK has sr_sync_type = 2(angle).
        /// Note: The value of the "angle values" flag must be equal for all DLBLOCKs in the linked list.
        /// Valid since MDF 4.2.0, should not be set for earlier versions
        /// </summary>
        AngleValues = 1 << 2,
        /// <summary>
        /// Distance values flag.
        /// If set, the DLBLOCK contains a list of (raw) distance values in dl_distance_values which can be used to improve performance for binary search of distance values in the list of referenced data blocks.
        /// The bit must not be set if the DLBLOCK references signal data blocks or if the DLBLOCK contains records from multiple channel groups (i.e. if parent is an unsorted data group). The bit can only be set if the channel group which defines the record layout contains either a virtual or a non-virtual distance master channel (cn_type = 2 or 3 and cn_sync_type = 3). Note that for RDBLOCKs, it only makes sense to store the distance values if the parent SRBLOCK has sr_sync_type = 3(distance).
        /// Note: The value of the "distance values" flag must be equal for all DLBLOCKs in the linked list.
        /// Valid since MDF 4.2.0, should not be set for earlier versions
        /// </summary>
        DistanceValues = 1 << 3,
    }

    /// <summary>
    /// Data List Block
    /// </summary>
    internal class DLBlock : BaseBlock
    {
        /// <summary>
        /// Pointers to the data blocks (DTBLOCK, SDBLOCK or RDBLOCK or a DZBLOCK of the respective block type). None of the links in the list can be NIL.
        /// It is not allowed to mix the data block types: all links must uniformly reference the same data block type(DTBLOCK/SDBLOCK/RDBLOCK or an equivalent DZBLOCK). Also all DLBLOCKs in the linked list of DLBLOCKs must reference the same data block type
        /// Note: a mixture between DZBLOCKs and uncompressed data blocks is allowed.However, if a DZBLOCK is in the list, the complete linked list of DLBLOCKs must be preceded by a HLBLOCK.See also explanation of HLBLOCK.
        /// </summary>
        public Int64 dl_dataGet(int index) => links[(int)LinkEnum.linkcount + index];
        public void dl_dataSet(int index, Int64 value) => links[(int)LinkEnum.linkcount + index] = value;

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Flags
            /// Bit combination of flags as defined below in
            /// Table 74. Note: The value of dl_flags must be equal for all DLBLOCKs in the linked list.
            /// </summary>
            public DLFlags dl_flags;

            /// <summary>
            /// Reserved.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 3)]
            byte[] dl_reserved;

            /// <summary>
            /// Number of referenced blocks N.
            /// If the "equal length" flag(bit 0 in dl_flags) is set, then dl_count must be equal for each DLBLOCK in the linked list except for the last one.For the last DLBLOCK(i.e.dl_dl_next = NIL) in this case the value of dl_count can be less than or equal to dl_count of the previous DLBLOCK.
            /// </summary>
            public UInt32 dl_count;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct ValueRecord
        {
            [FieldOffset(0)]
            public double AsDouble;
            [FieldOffset(0)]
            public Int64 AsInt64;
        }

        internal class dynamicData<T>
        {
            BlockData mainData;
            DLFlags ControlFlag;
            bool FlagValue = true;

            public T[] data;
            public UInt32 length => (mainData.dl_flags.HasFlag(ControlFlag) == FlagValue) ? mainData.dl_count : 0;

            public void UpdateLength() => Array.Resize(ref data, (int)length);

            public void Read(byte[] source, ref int offset)
            {
                UpdateLength();
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = source.ConvertTo(typeof(T), offset);
                    offset += Marshal.SizeOf(data[i]);
                }
            }

            public dynamicData(BlockData mainData, DLFlags ControlFlag, bool FlagValue = true)
            {
                this.mainData = mainData;
                this.ControlFlag = ControlFlag;
                this.FlagValue = FlagValue;
            }
        }

        /// <summary>
        /// Only present if "equal length" flag (bit 0 in dl_flags) is set.
        /// Equal data section length.
        /// Every block in dl_data list has a data section with a length equal to dl_equal_length. This must be true for each DLBLOCK within the linked list, and has only one exception: the very last block (dl_data[dl_count - 1] of last DLBLOCK in linked list) may have a data section with a different length which must be less than or equal to dl_equal_length.
        /// </summary>
        public UInt64 dl_equal_length;

        /// <summary>
        /// Only present if "equal length" flag (bit 0 in dl_flags) is not set.
        /// Start offset(in Bytes) for the data section of each referenced block.
        /// If the(uncompressed) data sections of all blocks referenced by dl_data list(for all DLBLOCKs in the linked list) are concatenated, the start offset for a referenced block gives the position of the data section of this block within the concatenated section.
        /// The start offset dl_offset[i] thus is equal to the sum of the data section lengths of all referenced blocks in dl_data list for every previous DLBLOCK in the linked list, and of all referenced blocks in dl_data list up to (i-1) for the current DLBLOCK. As a consequence, the start offset dl_offset [0] for the very first DLBLOCK must always be zero.
        /// </summary>
        public dynamicData<UInt64> dl_offset;

        /// <summary>
        /// Only present if "time values" flag (bit 1 in dl_flags) is set.
        /// The list stores the(raw) time values of the first record in each referenced data block.In case of RDBLOCKs, the interval start time is used, i.e.the (raw) time value in sub record 1 (see Table 68). Note that there is no use case for dl_time_values in case the DLBLOCK references signal data blocks(SDBLOCK or respective DZBLOCK).
        /// "First" record means the first record which starts in the data block, i.e. whose first byte is contained. Although not recommended, a record may be distributed to two or more data blocks. In such a case, if no record start byte is contained in the data block, the time value of the current (single, partly contained) record in this data block is used. dl_time_values[i] maps to the data block referenced by dl_data[i]. It is the raw value of the (virtual or non-virtual) time master channel (channel with cn_type = 2 or 3 and cn_sync_type = 1) in the channel group that defines the layout of the records stored in the data blocks (sorted data group required). For a virtual time master channel, the raw value is equivalent to the zero-based record index.
        /// The raw values are stored either as INT64 or REAL, depending if the data type of the time channel is an Integer or a Floating-point type(see cn_data_type). This is independent of the actually used number of bits(cn_bit_count) in the time master channel CNBLOCK.In order to get the physical time value, the conversion rule(if present) of the time master channel must be applied.
        /// </summary>
        public dynamicData<ValueRecord> dl_time_values;

        /// <summary>
        /// Only present if "angle values" flag (bit 2 in dl_flags) is set.
        /// Like dl_time_values, but here the raw values of the(virtual or non-virtual) angle master channel(channel with cn_type = 2 or 3 and cn_sync_type = 2) are stored.For details please refer to dl_time_values.
        /// </summary>
        public dynamicData<ValueRecord> dl_angle_values;

        /// <summary>
        /// Only present if "distance values" flag (bit 3 in dl_flags) is set.
        /// Like dl_time_values, but here the raw values of the(virtual or non-virtual) distance master channel(channel with cn_type = 2 or 3 and cn_sync_type = 3) are stored.For details please refer to dl_time_values.
        /// </summary>
        public dynamicData<ValueRecord> dl_distance_values;

        internal override int extraObjSize =>
            (int)((data.dl_flags.HasFlag(DLFlags.EqualLength) ? Marshal.SizeOf(dl_equal_length) : 0) +
            (dl_offset.length +
            dl_time_values.length +
            dl_angle_values.length +
            dl_distance_values.length) * Marshal.SizeOf(typeof(ValueRecord)));

        UInt32 dl_count
        {
            get => data.dl_count;
            set
            {
                data.dl_count = value;
                dl_offset.UpdateLength();
                dl_time_values.UpdateLength();
                dl_angle_values.UpdateLength();
                dl_distance_values.UpdateLength();
            }
        }

        public UInt64 CurrentOffset = 0;
        public IDataBlock CurrentDataBlock;

        public IDataBlock CreateNewData(BlockBuilder builder) =>
            CurrentDataBlock = MDF.UseCompression ? builder.BuildDZ(this, 0) : builder.BuildDT(this, 0);

        public void AppendCurrentDataBlock()
        {
            BaseBlock bb = CurrentDataBlock as BaseBlock;
            dl_count++;
            dl_offset.data[dl_count - 1] = CurrentOffset;
            LinkCount++;
            links.SetObject(links.Count - 1, bb);

            CurrentOffset += CurrentDataBlock.OrigDatalength;
        }

        internal void UpdateParent(DGBlock dg)
        {
            var block = dg.links.GetObject(DGLinks.dg_data);
            if (block is null)
                dg.links.SetObject(DGLinks.dg_data, this);
            else if (block is HLBlock hl)
            {
                hl.links.SetObject(HLLinks.hl_dl_first, this);
            }
            else if (block is DLBlock dl) // not used
            {
                //links.SetObject(DLLinks.dl_dl_next, dl);
                dg.links.SetObject(DGLinks.dg_data, this);
            }
        }


        // Objects to direct access childs
        public DLBlock dl_next => links.GetObject(LinkEnum.dl_dl_next);

        public DLBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
            dl_offset = new(data, DLFlags.EqualLength, false);
            dl_time_values = new(data, DLFlags.TimeValues);
            dl_angle_values = new(data, DLFlags.AngleValues);
            dl_distance_values = new(data, DLFlags.DistanceValues);
        }

        internal override void PostProcess()
        {
            int offset = 0;
            if (data.dl_flags.HasFlag(DLFlags.EqualLength))
            {
                dl_equal_length = extraObj.ConvertTo(dl_equal_length.GetType(), offset);
                offset += Marshal.SizeOf(dl_equal_length);
            }

            dl_offset.Read(extraObj, ref offset);
            dl_time_values.Read(extraObj, ref offset);
            dl_angle_values.Read(extraObj, ref offset);
            dl_time_values.Read(extraObj, ref offset);
        }

        public override byte[] ToBytes()
        {
            int size =
                (int)((data.dl_flags.HasFlag(DLFlags.EqualLength) ? Marshal.SizeOf(dl_equal_length) : 0) +
                (1 - data.dl_flags.HasFlag(DLFlags.EqualLength).AsByte() +
                data.dl_flags.HasFlag(DLFlags.TimeValues).AsByte() +
                data.dl_flags.HasFlag(DLFlags.AngleValues).AsByte() +
                data.dl_flags.HasFlag(DLFlags.DistanceValues).AsByte()) * dl_count * Marshal.SizeOf(typeof(ValueRecord)));

            extraObj = new byte[size];

            byte[] tmp;
            int offset = 0;
            if (data.dl_flags.HasFlag(DLFlags.EqualLength))
            {
                tmp = Bytes.ObjectToBytes(dl_equal_length);
                Array.Copy(tmp, 0, extraObj, offset, tmp.Length);
                offset += tmp.Length;
            }

            tmp = Bytes.ArrayToBytes(dl_offset.data);
            if (tmp is not null)
            {
                Array.Copy(tmp, 0, extraObj, offset, tmp.Length);
                offset += tmp.Length;
            }

            tmp = Bytes.ArrayToBytes(dl_time_values.data);
            if (tmp is not null)
            {
                Array.Copy(tmp, 0, extraObj, offset, tmp.Length);
                offset += tmp.Length;
            }

            tmp = Bytes.ArrayToBytes(dl_angle_values.data);
            if (tmp is not null)
            {
                Array.Copy(tmp, 0, extraObj, offset, tmp.Length);
                offset += tmp.Length;
            }

            tmp = Bytes.ArrayToBytes(dl_distance_values.data);
            if (tmp is not null)
                Array.Copy(tmp, 0, extraObj, offset, tmp.Length);

            return base.ToBytes();
        }

    };
}
