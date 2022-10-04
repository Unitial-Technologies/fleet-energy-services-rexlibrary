using InfluxShared.Generic;
using System;
using System.Runtime.InteropServices;

namespace MDF4xx.Blocks
{
    using LinkEnum = CCLinks;
    enum CCLinks
    {
        /// <summary>
        /// Link to TXBLOCK with name (identifier) of conversion (can be NIL). Name must be according to naming rules stated in Naming Rules.
        /// </summary>
        cc_tx_name,
        /// <summary>
        /// Link to TXBLOCK/MDBLOCK with physical unit of signal data (after conversion). (can be NIL) Unit only applies if no unit defined in CNBLOCK. 
        /// Otherwise the unit of the channel overwrites the conversion unit. An MDBLOCK can be used to additionally reference the HDO unit definition (see Table 61).
        /// Note: for channels with cn_sync_type > 0, the unit is already defined, thus a reference to an HDO definition should be omitted to avoid redundancy.
        /// </summary>
        cc_md_unit,
        /// <summary>
        /// Link to TXBLOCK/MDBLOCK with comment of conversion and additional information, see Table 60.(can be NIL)
        /// </summary>
        cc_md_comment,
        /// <summary>
        /// Link to CCBLOCK for inverse formula (can be NIL, must be NIL for CCBLOCK of the inverse formula (no cyclic reference allowed).
        /// </summary>
        cc_cc_inverse,
        linkcount
    };

    enum ConversionType : byte
    {
        /// <summary>
        /// 1:1 conversion (in this case, the CCBLOCK can be omitted)
        /// </summary>
        Identical,
        /// <summary>
        /// linear conversion
        /// </summary>
        Linear,
        /// <summary>
        /// rational conversion
        /// </summary>
        Rational,
        /// <summary>
        /// algebraic conversion (MCD-2 MC text formula)
        /// </summary>
        Formula,
        /// <summary>
        /// value to value tabular look-up with interpolation
        /// </summary>
        tblValueToValueInt,
        /// <summary>
        /// value to value tabular look-up without interpolation
        /// </summary>
        tblValueToValue,
        /// <summary>
        /// value range to value tabular look-up
        /// </summary>
        tblRangeToVal,
        /// <summary>
        /// value to text/scale conversion tabular look-up
        /// </summary>
        tblValueToText,
        /// <summary>
        /// value range to text/scale conversion tabular look-up
        /// </summary>
        tblRangeToText,
        /// <summary>
        /// text to value tabular look-up
        /// </summary>
        tblTextToValue,
        /// <summary>
        /// text to text tabular look-up (translation)
        /// </summary>
        tblTextToText,
        /// <summary>
        /// bitfield text table
        /// </summary>
        tblBitfieldText
    }

    /// <summary>
    /// Channel Conversion Block
    /// </summary>
    partial class CCBlock : BaseBlock
    {
        /// <summary>
        /// List of additional links to TXBLOCKs with strings or to CCBLOCKs with partial conversion rules. Length of list is given by cc_ref_count. The list can be empty. 
        /// Details are explained in formula-specific block supplement.
        /// </summary>
        public Int64 cc_refGet(int index) => links[(int)LinkEnum.linkcount + index];
        public void cc_refSet(int index, Int64 value) => links[(int)LinkEnum.linkcount + index] = value;

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class BlockData
        {
            /// <summary>
            /// Conversion type (formula identifier)
            /// </summary>
            internal ConversionType cc_type;

            /// <summary>
            /// Precision for display of floating-point values.
            /// 0xFF means unrestricted precision(infinite) Any other value specifies the number of decimal places to use for display of floating-point values.
            /// Note: only valid if "precision valid" flag (bit 0) is set and if cn_precision of the parent CNBLOCK is invalid, otherwise cn_precision must be used.
            /// </summary>
            public byte cc_precision;

            /// <summary>
            /// Flags
            /// The value contains the following bit flags(Bit 0 = LSB) :
            /// <br/>Bit 0: Precision valid flag -
            /// If set, the precision value for display of floating-point values specified in cc_precision is valid
            /// <br/>Bit 1: Physical value range valid flag -
            /// If set, both the minimum and the maximum physical value that occurred after conversion for this signal within the samples recorded in this file are known and 
            /// stored in cc_phy_range_min and cc_phy_range_max.Otherwise the two fields are not valid.Note: the physical value range can only be expressed for conversions 
            /// which return a numeric value(REAL). For conversions returning a string value or for the inverse conversion rule the flag must not be set.
            /// <br/>Bit 2: Status string flag -
            /// This flag indicates for conversion types 7 and 8 (value/value range to text/scale conversion tabular look-up) that the normal table entries are status 
            /// strings (only reference to TXBLOCK), and the actual conversion rule is given in CCBLOCK referenced by default value.This also implies special handling of limits, 
            /// see MCD-2 MC[1] keyword STATUS_STRING_REF. Can only be set for 7 ≤ cc_type ≤ 8.
            /// </summary>
            public UInt16 cc_flags;

            /// <summary>
            /// Length M of cc_ref list with additional links. See formula-specific block supplement for meaning of the links.
            /// </summary>
            public UInt16 cc_ref_count;

            /// <summary>
            /// Length N of cc_val list with additional parameters. See formula-specific block supplement for meaning of the parameters.
            /// </summary>
            public UInt16 cc_val_count;

            /// <summary>
            /// Minimum physical signal value that occurred for this signal. Only valid if "physical value range valid" flag (bit 1) is set.
            /// </summary>
            public double cc_phy_range_min;

            /// <summary>
            /// Maximum physical signal value that occurred for this signal. Only valid if "physical value range valid" flag (bit 1) is set.
            /// </summary>
            public double cc_phy_range_max;
        }

        /// <summary>
        /// Data block
        /// </summary>
        internal BlockData data { get => (BlockData)dataObj; set => dataObj = value; }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct ValueRecord
        {
            //[FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
            //public byte[] AsBytes;
            [FieldOffset(0)]
            public double AsDouble;
            [FieldOffset(0)]
            public UInt64 AsInt64;
        }

        /// <summary>
        /// List of additional conversion parameters. Length of list is given by cc_val_count. The list can be empty. Details are explained in formula-specific block supplement.
        /// </summary>
        public ValueRecord[] cc_val;

        public UInt16 cc_val_length
        {
            get => data.cc_val_count;
            set
            {
                Array.Resize(ref cc_val, value);
                data.cc_val_count = value;
            }
        }

        internal override int extraObjSize => cc_val_length * Marshal.SizeOf(typeof(ValueRecord));

        public ConversionType ConvertType
        {
            get => data.cc_type;
            set
            {
                data.cc_type = value;
                UpdateConvertMethod();
            }
        }

        // Objects to direct access childs
        public TXBlock tx_name => links.GetObject(LinkEnum.cc_tx_name);
        public MDBlock md_unit => links.GetObject(LinkEnum.cc_md_unit);
        public MDBlock md_comment => links.GetObject(LinkEnum.cc_md_comment);
        public CCBlock cc_inverse => links.GetObject(LinkEnum.cc_cc_inverse);

        public CCBlock(HeaderSection hs = null) : base(hs)
        {
            LinkCount = (hs is null) ? (int)LinkEnum.linkcount : hs.link_count;
            data = new BlockData();
            Calculate = CalcIdentical;
        }

        internal override void PostProcess()
        {
            cc_val_length = (UInt16)(extraObjSize / Marshal.SizeOf(typeof(ValueRecord)));
            Bytes.BytesToArray(extraObj, cc_val, cc_val_length * (uint)Marshal.SizeOf(typeof(ValueRecord)));
        }

        public override byte[] ToBytes()
        {
            extraObj = Bytes.ArrayToBytes(cc_val, cc_val_length * Marshal.SizeOf(typeof(ValueRecord)));
            return base.ToBytes();
        }

    };
}
