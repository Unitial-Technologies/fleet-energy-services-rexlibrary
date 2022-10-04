using InfluxShared.Helpers;
using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
	using LinkEnum = CNLinks;
	enum CNLinks
	{
		/// <summary>
		/// Pointer to the first data group block (DGBLOCK) (can be NIL)
		/// </summary>
		cn_cn_next,
		/// <summary>
		/// Composition of channels: Pointer to channel array block (CABLOCK) or channel block (CNBLOCK) (can be NIL). Details see 4.18 Composition of Channels.
		/// </summary>
		cn_composition,
		/// <summary>
		/// Pointer to TXBLOCK with name (identification) of channel. Name must be according to naming rules stated in Naming Rules.
		/// The combination of name and source name and path(both from SIBLOCK) must be unique within all channels of this channel group. See also 4.4.3 Identification of Channels.
		/// Note: Alternative names (e.g.display name) can be stored in MDBLOCK of cn_md_comment, see Table 41.
		/// </summary>
		cn_tx_name,
		/// <summary>
		/// Pointer to channel source (SIBLOCK) (can be NIL)
		/// Must be NIL for component channels(members of a structure or array elements) because they all must have the same source and thus simply use the SIBLOCK of 
		/// their parent CNBLOCK(direct child of CGBLOCK).
		/// See also 4.4.3 Identification of Channels.
		/// </summary>
		cn_si_source,
		/// <summary>
		/// Pointer to the conversion formula (CCBLOCK) (can be NIL, must be NIL for complex channel data types, i.e. for cn_data_type ≥ 10). 
		/// If the pointer is NIL, this means that a 1:1 conversion is used (phys = int).
		/// </summary>
		cn_cc_conversion,
		/// <summary>
		/// Pointer to channel type specific signal data
		/// - For variable length data channel (cn_type = 1): unique link to signal data block (SDBLOCK or DZBLOCK for this block type) or data list 
		/// block (DLBLOCK or HLBLOCK if required) or, only for unsorted data groups, referencing link to a VLSD channel group block (CGBLOCK). Can only be NIL if SDBLOCK would be empty.
		/// - For synchronization channel(cn_type = 4) : referencing link to attachment block(ATBLOCK) in global linked list of ATBLOCKs starting at hd_at_first.Cannot be NIL.
		/// - For maximum length data channel (cn_type = 5): referencing link to channel block(CNBLOCK) in same channel group.Cannot be NIL.
		/// - For the event signal structure referencing link to the template event block (EVBLOCK). Cannot be NIL.See chapter 4.12.5 Event Signals for details.
		/// Must be NIL in all other cases. See respective channel type for further details.
		/// </summary>
		cn_data,
		/// <summary>
		/// Pointer to TXBLOCK/MDBLOCK with designation for physical unit of signal data (after conversion) or (only for channel data types "MIME sample" and "MIME stream") 
		/// to MIME context-type text. (can be NIL).
		/// - The unit can be used if no conversion rule is specified or to overwrite the unit specified for the conversion rule(e.g. if a conversion rule is shared between channels). 
		/// If the link is NIL, then the unit from the conversion rule must be used.If the content is an empty string, no unit should be displayed.If an MDBLOCK is used, in addition 
		/// the HDO unit definition can be stored, see Table 42. 
		/// Note: for (virtual) master and synchronization channels the HDO definition should be omitted to avoid redundancy. Here the unit is already specified by cn_sync_type of the channel.
		/// - In case of channel data types "MIME sample" and "MIME stream", the text of the unit must be the content-type text of a MIME type which specifies the content of the values 
		/// of the channel(either fixed length in record or variable length in SDBLOCK). The MIME content - type string must be written in lowercase, and it must apply to the same rules 
		/// as defined for at_tx_mimetype in 4.11 The Attachment Block ATBLOCK.
		/// </summary>
		cn_md_unit,
		/// <summary>
		/// Pointer to TXBLOCK/MDBLOCK with comment and additional information about the channel, see Table 41. (can be NIL)
		/// </summary>
		cn_md_comment,
		linkcount
	}

	enum CNType : byte
	{
		/// <summary>
		/// 0 = fixed length data channel 
		/// <br/>channel value is contained in record.
		/// </summary>
		FixedLength,
		/// <summary>
		/// 1 = variable length data channel also denoted as "variable length signal data" (VLSD)channel
		/// <br/>The channel value in the record is an unsigned integer value(LE / Intel Byte order) of the specified number of bits.This value is the
		/// offset to a variable length signal value in the data section of the SDBLOCK referenced by cn_data.Channel data type and CC rule refer
		/// to data in SDBLOCK, not to record data. Signal data must point to an SDBLOCK or a DLBLOCK with list of SDBLOCKs. For unsorted data 
		/// groups, alternatively it can point to a VLSD CGBLOCK, see explanation in 5.14.3 Variable Length Signal Data(VLSD) CGBLOCK.
		/// </summary>
		VLSD,
		/// <summary>
		/// 2 = master channel for all signals of this group
		/// <br/>Must be combined with one of the following sync types : 1, 2, 3 (see table line for cn_sync_type).
		/// In each channel group, not more than one channel can be defined as master or virtual master channel for each possible sync type.
		/// Value decoding is equal to a fixed length data channel.
		/// Physical values of this channel must return the SI unit(see[SI]) of the respective sync type, with or without CCBLOCK, i.e.seconds for a
		/// time master channel.
		/// Values of this channel listed over the record index must be strictly monotonic increasing. They are always relative to the respective start
		/// value in the HDBLOCK. See also section 5.4.5 Synchronization Domains.
		/// </summary>
		MasterChannel,
		/// <summary>
		/// 3 = virtual master channel
		/// <br/>Like a master channel, except that the channel value is not contained in the record (cn_bit_count must be zero).Instead the physical 
		/// value must be calculated by feeding the zero - based record index to the conversion rule.The data type of the virtual master channel 
		/// must be unsigned integer with Little Endian byte order(cn_data_type = 0).
		/// Except of this, the same rules apply as for a master channel(cn_type = 2).
		/// </summary>
		VirtualMasterChannel,
		/// <summary>
		/// 4 = synchronization channel
		/// <br/>Must be combined with one of the following sync types : 1, 2, 3, 4 (see table line for cn_sync_type).
		/// A synchronization channel is used to synchronize the records of this channel group with samples in some other stream, e.g.AVI frames.
		/// Physical values of this channel must return the unit of the respective synchronization domain, i.e.seconds for cn_sync_type = 1, or 
		/// an index value for cn_sync_type = 4. These values are used for synchronization with the stream.Hence, the stream must use the same 
		/// synchronization domain, i.e.a data row with synchronization values or index - based samples(only for cn_sync_type = 4).
		/// The signal data link(cn_data) must refer to an ATBLOCK in the global list of attachments. The ATBLOCK contains the stream data or a
		/// reference to a stream file. See also section 5.4.5 Synchronization Domains.
		/// </summary>
		SynchronizationChannel,
		/// <summary>
		/// 5 = maximum length data channel also denoted as "maximum length signal data" (MLSD) channel
		/// <br/>Record contains range with maximum number of Bytes for channel value like for a fixed length data channel(cn_type = 0), but the number of currently valid Bytes 
		/// is given by the value of a channel referenced by cn_data(denoted as "size signal").
		/// Since the size signal defines the number of Bytes to use, its physical values must be Integer values ≥ 0 and ≤ (cn_bit_count >> 3), i.e.its values 
		/// must not exceed the number of Bytes reserved in the record for this channel. Usually, the size signal should have some Integer data type(cn_data_type ≤ 3) 
		/// without conversion rule and without unit. The value of the size signal must be contained in the same record as the current channel, i.e.both channels must be 
		/// in the same channel group.
		/// Note: this channel type must not be used with numeric or CANopen data types, i.e.it can occur only for (6 ≤ cn_data_type ≤ 12).
		/// valid since MDF 4.1.0, should not occur for earlier versions
		/// </summary>
		MLSD,
		/// <summary>
		/// 6 = virtual data channel
		/// <br/>Similar to a virtual master channel the channel value is not contained in the record (cn_bit_count must be zero).Instead the physical 
		/// value must be calculated by feeding the zero - based record index to the conversion rule.The data type of the virtual master channel 
		/// must be unsigned integer with Little Endian byte order(cn_data_type = 0). Except of this, the same rules apply as for a fixed length 
		/// data channel(cn_type = 0). A virtual data channel may be used to specify a channel with constant values without consuming any space 
		/// in the record.The constant value can be given by the offset of a linear conversion rule with factor equal to zero.
		/// Valid since MDF 4.1.0, should not occur for earlier versions
		/// </summary>
		VirtualDataChannel
	}

	enum CNSyncType : byte
	{
		/// <summary>
		/// 0 = None(to be used for normal data channels)
		/// </summary>
		None,
		/// <summary>
		/// 1 = Time(physical values must be seconds)
		/// </summary>
		Time,
		/// <summary>
		/// 2 = Angle(physical values must be radians)
		/// </summary>
		Angle,
		/// <summary>
		/// 3 = Distance(physical values must be meters)
		/// </summary>
		Distance,
		/// <summary>
		/// 4 = Index(physical values must be zero-based index values)
		/// </summary>
		Index
	}

	enum CNDataType : byte
	{
		/// <summary>
		/// 0 = unsigned integer(LE Byte order) 
		/// </summary>
		IntelUnsigned,
		/// <summary>
		/// 1 = unsigned integer(BE Byte order) 
		/// </summary>
		MotorolaUnsigned,
		/// <summary>
		/// 2 = signed integer(two’s complement) (LE Byte order) 
		/// </summary>
		IntelSigned,
		/// <summary>
		/// 3 = signed integer(two’s complement) (BE Byte order) 
		/// </summary>
		MotorolaSigned,
		/// <summary>
		/// 4 = IEEE 754 floating-point format(LE Byte order) 
		/// </summary>
		IntelFloat,
		/// <summary>
		/// 5 = IEEE 754 floating-point format(BE Byte order)
		/// </summary>
		MotorolaFloat,
		/// <summary>
		/// 6 = string (SBC, standard ISO-8859-1 encoded(Latin), NULL terminated) 
		/// </summary>
		ASCII,
		/// <summary>
		/// 7 = string (UTF-8 encoded, NULL terminated) 
		/// </summary>
		UTF8,
		/// <summary>
		/// 8 = string (UTF-16 encoded LE Byte order, NULL terminated) 
		/// </summary>
		IntelUTF16,
		/// <summary>
		/// 9 = string (UTF-16 encoded BE Byte order, NULL terminated) 
		/// </summary>
		MotorolaUTF16,
		/// <summary>
		/// 10 = byte array with unknown content(e.g.structure) 
		/// </summary>
		ByteArray,
		/// <summary>
		/// 11 = MIME sample(sample is Byte Array with MIME content-type specified in cn_md_unit) 
		/// </summary>
		MimeSample,
		/// <summary>
		/// 12 = MIME stream(all samples of channel represent a stream with MIME content-type specified in cn_md_unit) 
		/// </summary>
		MimeStream,
		/// <summary>
		/// 13 = CANopen date(Based on 7 Byte CANopen Date data structure, see Table 39) 
		/// </summary>
		CANopenDate,
		/// <summary>
		/// 14 = CANopen time(Based on 6 Byte CANopen Time data structure, see Table 40)
		/// </summary>
		CANopenTime,
		/// <summary>
		/// 15 = complex number(real part followed by imaginary part, stored as two floating-point data, both with 2, 4 or 8 Byte, LE Byte order) valid since MDF 4.2.0
		/// </summary>
		IntelComplex,
		/// <summary>
		/// 16 = complex number(real part followed by imaginary part, stored as two floating-point data, both with 2, 4 or 8 Byte, BE Byte order) valid since MDF 4.2.0
		/// </summary>
		MotorolaComplex
	}

	[Flags]
	enum CNFlags : UInt32
	{
		/// <summary>
		/// Bit 0: All values invalid flag -
		/// If set, all values of this channel are invalid.If in addition an invalidation bit is used(bit 1 set), then the value of the invalidation bit must be set(high) 
		/// for every value of this channel.Must not be set for a master channel (channel types 2 and 3).
		/// </summary>
		AllValuesInvalid = 1 << 0,
		/// <summary>
		/// Bit 1: Invalidation bit valid flag -
		/// If set, this channel uses an invalidation bit(position specified by cn_inval_bit_pos).
		/// Must not be set if cg_inval_bytes is zero.Must not be set for a master channel(channel types 2 and 3).
		/// </summary>
		InvalidationBit = 1 << 1,
		/// <summary>
		/// Bit 2: Precision valid flag -
		/// If set, the precision value for display of floating - point values specified in cn_precision is valid and overrules a possibly specified precision value of 
		/// the conversion rule(cc_precision).
		/// </summary>
		ValidPrecision = 1 << 2,
		/// <summary>
		/// Bit 3: Value range valid flag -
		/// If set, both the minimum and the maximum raw value that occurred for this signal within the samples recorded in this file are known and stored in 
		/// cn_val_range_min and cn_val_range_max.Otherwise the two fields are not valid.
		/// Note: the raw value range can only be expressed for numeric channel data types(cn_data_type ≤ 5).For all other data types, the flag must not be set.
		/// </summary>
		ValidValueRange = 1 << 3,
		/// <summary>
		/// Bit 4: Limit range valid flag -
		/// If set, the limits of the signal value are known and stored in cn_limit_min and cn_limit_max.Otherwise the two fields are not valid. (see MCD - 2 MC[1] 
		/// keywords LowerLimit / UpperLimit for MEASUREMENT and CHARACTERISTIC)
		/// This limit range defines the range of plausible values for this channel and may be used to display a warning.
		/// Note: the(extended) limit range can only be expressed for a channel whose conversion rule returns a numeric value(REAL) or which has a numeric channel data type
		/// (cn_data_type ≤ 5).In all other cases, the flag must not be set.Depending on the type of conversion, the limit values are interpreted as physical or raw values.
		/// If the conversion rule results in numeric values, the limits must be interpreted as physical values, otherwise(e.g. for verbal / scale conversion) as raw values.
		/// </summary>
		ValidLimitRange = 1 << 4,
		/// <summary>
		/// Bit 5: Extended limit range valid flag -
		/// If set, the extended limits of the signal value are known and stored in cn_limit_ext_min and cn_limit_ext_max.Otherwise the two fields are not valid. 
		/// (see MCD - 2 MC[1] keyword EXTENDED_LIMITS) The extended limit range must be larger or equal to the limit range(if valid). Values outside the extended limit range 
		/// indicate an error during measurement acquisition. The extended limit range can be used for any limits, e.g.physical ranges from analog sensors.
		/// See also remarks for "limit range valid" flag(bit 4).
		/// </summary>
		ValidExtendedLimitRange = 1 << 5,
		/// <summary>
		/// Bit 6: Discrete value flag -
		/// If set, the signal values of this channel are discrete and must not be interpolated. (see MCD - 2 MC[1] keyword DISCRETE)
		/// </summary>
		DiscreteValue = 1 << 6,
		/// <summary>
		/// Bit 7: Calibration flag -
		/// If set, the signal values of this channel correspond to a calibration object, otherwise to a measurement object(see MCD - 2 MC[1] keywords MEASUREMENT and CHARACTERISTIC)
		/// </summary>
		Calibration = 1 << 7,
		/// <summary>
		/// Bit 8: Calculated flag -
		/// If set, the values of this channel have been calculated from other channel inputs. (see MCD - 2 MC[1] keywords VIRTUAL and DEPENDENT_CHARACTERISTIC) In MDBLOCK for 
		/// cn_md_comment the used input signals and the calculation formula can be documented, see Table 41.
		/// </summary>
		Calculated = 1 << 8,
		/// <summary>
		/// Bit 9: Virtual flag -
		/// If set, this channel is virtual, i.e.it is simulated by the recording tool. (see MCD-2 MC[1] keywords VIRTUAL and VIRTUAL_CHARACTERISTIC) 
		/// Note: for a virtual measurement according to MCD-2 MC both the "Virtual" flag(bit 9) and the "Calculated" flag(bit 8) should be set.
		/// </summary>
		Virtual = 1 << 9,
		/// <summary>
		/// Bit 10: Bus event flag -
		/// If set, this channel contains information about a bus event. For details please refer to MDF Bus Logging [7]. valid since MDF 4.1.0, should not be set for earlier versions
		/// </summary>
		BusEvent = 1 << 10,
		/// <summary>
		/// Bit 11: Strictly monotonous flag -
		/// If set, this channel contains only strictly monotonously increasing / decreasing values.The flag is optional. valid since MDF 4.1.0, should not be set for earlier versions
		/// </summary>
		StrictlyMonotonous = 1 << 11,
		/// <summary>
		/// Bit 12: Default X axis flag -
		/// If set, a channel to be preferably used as X axis is specified by cn_default_x.This is only a recommendation; a tool may choose to use a different X axis.
		/// valid since MDF 4.1.0, should not be set for earlier versions
		/// </summary>
		DefaultXAxis = 1 << 12,
		/// <summary>
		/// Bit 13: Event signal flag -
		/// If set, a channel is used for the description of events in an event signal group. See chapter 4.12.5 Event Signals for details. 
		/// Valid since MDF 4.2.0, should not be set for earlier versions
		/// </summary>
		EventSignal = 1 << 13,
		/// <summary>
		/// Bit 14: VLSD data stream flag -
		/// Can only be set for a variable length data channel(cn_type = 1) in a sorted data group.
		/// If set, the SDBLOCK referenced by cn_data (or its equivalent in case of distributed or compressed data blocks) contains a stream of VLSD values, i.e.all 
		/// VLSD values(each consisting of 4 Byte length and N Bytes data) must be stored in correct ordering and without gaps.In this case, for reading all VLSD values, 
		/// the offset stored in fixed-length record for VLSD channel can be ignored because the VLSD values can be read one - by - one from the data "stream".
		/// The flag is optional. valid since MDF 4.2.0, should not be set for earlier versions
		/// </summary>
		VLSD = 1 << 14
	}

	/// <summary>
	/// Channel Block
	/// </summary>
	class CNBlock : BaseBlock
	{
		/// <summary>
		/// List of attachments for this channel (references to ATBLOCKs in global linked list of ATBLOCKs).
		/// The length of the list is given by cn_attachment_count.It can be empty(cn_attachment_count = 0), i.e.there are no attachments for this channel.valid since MDF 4.1.0
		/// </summary>
		public Int64 cn_at_referenceGet(int index) => links[(int)LinkEnum.linkcount + index];
		public void cn_at_referenceSet(int index, Int64 value) => links[(int)LinkEnum.linkcount + index] = value;

		public struct DefaultXRecord
		{
			public Int64 dgBlock;
			public Int64 cgBlock;
			public Int64 cnBlock;
		}

		/// <summary>
		/// Only present if "default X" flag (bit 12) is set. Reference to channel to be preferably used as X axis.
		/// The reference is a link triple with pointer to parent DGBLOCK, parent CGBLOCK and CNBLOCK for the channel (none of them must be NIL).
		/// The referenced channel does not need to have the same raster nor monotonously increasing values.It can be a master channel, e.g. in case several master channels are present.
		/// In case of different rasters, visualization may depend on the interpolation method used by the tool. In case no default X channel is specified, the tool is free to choose 
		/// the X axis; usually a master channels would be used. Valid since MDF 4.1.0
		/// </summary>
		public DefaultXRecord cn_default_x
		{
			get => new DefaultXRecord()
			{
				dgBlock = links[(int)LinkEnum.linkcount + data.cn_attachment_count],
				cgBlock = links[(int)LinkEnum.linkcount + 1 + data.cn_attachment_count],
				cnBlock = links[(int)LinkEnum.linkcount + 2 + data.cn_attachment_count]
			};
			set
			{
				links[(int)LinkEnum.linkcount + data.cn_attachment_count] = value.dgBlock;
				links[(int)LinkEnum.linkcount + 1 + data.cn_attachment_count] = value.cgBlock;
				links[(int)LinkEnum.linkcount + 2 + data.cn_attachment_count] = value.cnBlock;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
		internal class BlockData
		{
			/// <summary>
			/// Channel type
			/// </summary>
			public CNType cn_type;

			/// <summary>
			/// Sync type
			/// <br/>See also section 4.4.6 Synchronization Domains.
			/// </summary>
			public CNSyncType cn_sync_type;

			/// <summary>
			/// Channel data type of raw signal value Integer data types
			/// </summary>
			public CNDataType cn_data_type;

			/// <summary>
			/// Bit offset (0-7): first bit (=LSB) of signal value after Byte offset has been applied (see 4.21.5.2 Reading the Signal Value). 
			/// If zero, the signal value is 1-Byte aligned. A value different to zero is only allowed for Integer data types (cn_data_type ≤ 3) 
			/// and if the Integer signal value fits into 8 contiguous Bytes (cn_bit_count + cn_bit_offset ≤ 64). For all other cases, cn_bit_offset must be zero.
			/// </summary>
			public byte cn_bit_offset;

			/// <summary>
			/// Offset to first Byte in the data record that contains bits of the signal value. The offset is applied to the plain record data, i.e. skipping the record ID.
			/// </summary>
			public UInt32 cn_byte_offset;

			/// <summary>
			/// Number of bits for signal value in record
			/// </summary>
			public UInt32 cn_bit_count;

			/// <summary>
			/// Flags
			/// </summary>
			public CNFlags cn_flags;

			/// <summary>
			/// Position of invalidation bit.
			/// The invalidation bit can be used to specify if the signal value in the current record is valid or not.
			/// Note: the invalidation bit is optional and can only be used if the "invalidation bit valid" flag(bit 1) is set.
			/// cn_inval_bit_pos contains both bit and Byte offset for the(single) invalidation bit and specifies its position within the invalidation Bytes of the record.
			/// This means that the record ID(if present) and the data Bytes must be skipped before applying the Byte offset(cn_inval_bit_pos shr 3). Within this Byte, 
			/// the number of the invalidation bit (starting from LSB = 0) is specified by (cn_inval_bit_pos &amp; 0x07).
			/// If an invalidation bit is used for a channel, and if the respective bit in the invalidation Bytes of the record is set(value is high), then the signal value of 
			/// this channel in the record is invalid.In this case, as best practice, the record should contain the most recent valid signal value for this channel, or zero if 
			/// no valid signal value has occurred yet. For an example please refer to 4.21.5.1 Reading the Invalidation Bit.
			/// </summary>
			public UInt32 cn_inval_bit_pos;

			/// <summary>
			/// Precision for display of floating-point values. 0xFF means unrestricted precision (infinite). Any other value specifies the number of decimal places to use for 
			/// display of floating-point values. Only valid if "precision valid" flag (bit 2) is set
			/// </summary>
			public byte cn_precision;

			/// <summary>
			/// Reserved
			/// </summary>
			byte cn_reserved;

			/// <summary>
			/// Length N of cn_at_reference list, i.e. number of attachments for this channel. Can be zero. Valid since MDF 4.1.0, should be zero for earlier versions
			/// </summary>
			public UInt16 cn_attachment_count;

			/// <summary>
			/// Minimum signal value that occurred for this signal (raw value) Only valid if "value range valid" flag (bit 3) is set.
			/// </summary>
			public double cn_val_range_min;

			/// <summary>
			/// Maximum signal value that occurred for this signal (raw value) Only valid if "value range valid" flag (bit 3) is set.
			/// </summary>
			public double cn_val_range_max;

			/// <summary>
			/// Lower limit for this signal (physical value for numeric conversion rule, otherwise raw value) Only valid if "limit range valid" flag (bit 4) is set.
			/// </summary>
			public double cn_limit_min;

			/// <summary>
			/// Upper limit for this signal (physical value for numeric conversion rule, otherwise raw value) Only valid if "limit range valid" flag (bit 4) is set.
			/// </summary>
			public double cn_limit_max;

			/// <summary>
			/// Lower extended limit for this signal (physical value for numeric conversion rule, otherwise raw value) Only valid if "extended limit range valid" flag (bit 5) is set.
			/// If cn_limit_min is valid, cn_limit_min must be larger or equal to cn_limit_ext_min.
			/// </summary>
			public double cn_limit_ext_min;

			/// <summary>
			/// Upper extended limit for this signal (physical value for numeric conversion rule, otherwise raw value) Only valid if "extended limit range valid" flag (bit 5) is set. 
			/// If cn_limit_max is valid, cn_limit_max must be less or equal to cn_limit_ext_max.
			/// </summary>
			public double cn_limit_ext_max;
		}

		/// <summary>
		/// Data block
		/// </summary>
		internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

		public UInt32 LastByteOffset => data.cn_byte_offset + (data.cn_bit_offset + data.cn_bit_count) / 8;
		public byte LastBitOffset => (byte)((data.cn_bit_offset + data.cn_bit_count) % 8);

		public bool GetFlag(CNFlags flag) => data.cn_flags.HasFlag(flag);
		public void SetFlag(CNFlags flag, bool value) => data.cn_flags = data.cn_flags.SetFlag(flag, value);

		// CN Flags
		public bool FlagAllValuesInvalid
		{
			get => GetFlag(CNFlags.AllValuesInvalid); 
			set => SetFlag(CNFlags.AllValuesInvalid, value);
		}
		public bool FlagInvalidationBit 
		{ 
			get => GetFlag(CNFlags.InvalidationBit); 
			set => SetFlag(CNFlags.InvalidationBit, value); 
		}
		public bool FlagValidPrecision
		{
			get => GetFlag(CNFlags.ValidPrecision); 
			set => SetFlag(CNFlags.ValidPrecision, value);
		}
		public bool FlagValidValueRange 
		{ 
			get => GetFlag(CNFlags.ValidValueRange);
			set => SetFlag(CNFlags.ValidValueRange, value); 
		}
		public bool FlagValidLimitRange 
		{ 
			get => GetFlag(CNFlags.ValidLimitRange); 
			set => SetFlag(CNFlags.ValidLimitRange, value); 
		}
		public bool FlagValidExtendedLimitRange
		{
			get => GetFlag(CNFlags.ValidExtendedLimitRange); 
			set => SetFlag(CNFlags.ValidExtendedLimitRange, value);
		}
		public bool FlagDiscreteValue 
		{ 
			get => GetFlag(CNFlags.DiscreteValue); 
			set => SetFlag(CNFlags.DiscreteValue, value);
		}
		public bool FlagCalibration 
		{
			get => GetFlag(CNFlags.Calibration); 
			set => SetFlag(CNFlags.Calibration, value); 
		}
		public bool FlagCalculated 
		{
			get => GetFlag(CNFlags.Calculated);
			set => SetFlag(CNFlags.Calculated, value); 
		}
		public bool FlagVirtual
		{
			get => GetFlag(CNFlags.Virtual);
			set => SetFlag(CNFlags.Virtual, value); 
		}
		public bool FlagBusEvent 
		{
			get => GetFlag(CNFlags.BusEvent); 
			set => SetFlag(CNFlags.BusEvent, value); 
		}
		public bool FlagStrictlyMonotonous
		{ 
			get => GetFlag(CNFlags.StrictlyMonotonous); 
			set => SetFlag(CNFlags.StrictlyMonotonous, value); 
		}
		public bool FlagDefaultXAxis 
		{
			get => GetFlag(CNFlags.DefaultXAxis); 
			set => SetFlag(CNFlags.DefaultXAxis, value); 
		}
		public bool FlagEventSignal 
		{ 
			get => GetFlag(CNFlags.EventSignal);
			set => SetFlag(CNFlags.EventSignal, value);
		}
		public bool FlagVLSD 
		{ 
			get => GetFlag(CNFlags.VLSD);
			set => SetFlag(CNFlags.VLSD, value); 
		}

		// Objects to direct access childs
		public CNBlock cn_next => links.GetObject(LinkEnum.cn_cn_next);
		public BaseBlock composition => links.GetObject(LinkEnum.cn_composition);
		public TXBlock tx_name => links.GetObject(LinkEnum.cn_tx_name);
		public SIBlock si_source => links.GetObject(LinkEnum.cn_si_source);
		public CCBlock cc_conversion => links.GetObject(LinkEnum.cn_cc_conversion);
		public BaseBlock cn_data => links.GetObject(LinkEnum.cn_data);
		public MDBlock md_unit => links.GetObject(LinkEnum.cn_md_unit);
		public MDBlock md_comment => links.GetObject(LinkEnum.cn_md_comment);

		public CNBlock(HeaderSection hs = null) : base(hs)
		{
			LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
			data = new BlockData();
		}

		public void AppendArrayChildCN(CNBlock newcn, CNBlock overlapped = null)
		{
			repeat:
			if (composition is null)
			{
				links.SetObject(LinkEnum.cn_composition, newcn);
			}
			else
			{
				CNBlock cn = (CNBlock)composition;
				while (cn.cn_next != null)
					cn = cn.cn_next;

				cn.links.SetObject(CNLinks.cn_cn_next, newcn);
			}
			if (overlapped is not null)
            {
				newcn = overlapped;
				overlapped = null;
				goto repeat;
            }

			data.cn_bit_count += newcn.data.cn_bit_count;
			UInt32 arrayendbit = Math.Max(
				newcn.data.cn_byte_offset * 8 + newcn.data.cn_bit_offset + newcn.data.cn_bit_count,
				data.cn_byte_offset * 8 + data.cn_bit_offset + data.cn_bit_count
			);
			data.cn_bit_count = arrayendbit - data.cn_byte_offset * 8 - data.cn_bit_offset;
		}

	};
}
