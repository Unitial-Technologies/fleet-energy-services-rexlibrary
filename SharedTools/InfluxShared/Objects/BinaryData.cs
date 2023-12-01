using InfluxShared.FileObjects;
using System;
using System.Runtime.InteropServices;

namespace InfluxShared.Objects
{
    public class BinaryData
    {
        public static readonly Type[] BinaryTypes = new Type[] { typeof(UInt64), typeof(Int64), typeof(Single), typeof(double) };
        static readonly byte[] BytePos = new byte[] { 0, 8, 16, 24, 32, 40, 48, 56 };

        [StructLayout(LayoutKind.Explicit)]
        public struct HexStruct
        {
            [FieldOffset(0)]
            public UInt64 Unsigned;
            [FieldOffset(0)]
            public Int64 Signed;
            [FieldOffset(0)]
            public float Single;
            [FieldOffset(0)]
            public double Double;

            public static explicit operator HexStruct(UInt64 UnsignedValue) => new HexStruct() { Unsigned = UnsignedValue };
        }

        public readonly UInt16 StartBit;
        public readonly UInt16 BitCount;
        public readonly bool isIntel;
        public readonly Type HexType;
        public readonly double Factor;
        public readonly double Offset;
        public readonly TableNumericConversion Table;

        // Precalculated variables
        readonly UInt16 byteOffset;
        readonly UInt16 bitOffset;
        readonly UInt64 bitMask;
        readonly UInt16 canBytes;
        readonly UInt64 signBitmask;

        private delegate UInt64 ByteReadMethod(byte[] arr, int Offset, int ByteCount);
        readonly ByteReadMethod ByteRead;
        public delegate double CalcValueMethod(ref HexStruct hex);
        public readonly CalcValueMethod CalcValue;

        public BinaryData(UInt16 StartBit, UInt16 BitCount, bool isIntel, int DataTypeIndex)
        {
            this.StartBit = StartBit;
            this.BitCount = BitCount;
            this.isIntel = isIntel;
            this.HexType = BinaryTypes[DataTypeIndex];

            // Calculate channel binary helpers
            byteOffset = (UInt16)(StartBit >> 3);
            bitOffset = (UInt16)((isIntel ? StartBit : 65u - BitCount + StartBit) & 7u);
            bitMask = ~(UInt64)0u >> (64 - BitCount);
            canBytes = (UInt16)((BitCount + bitOffset + 7) >> 3);
            signBitmask = (UInt64)1 << (BitCount - 1);

            if (isIntel)
                ByteRead = ByteReadIntel;
            else
                ByteRead = ByteReadMotorola;

            switch (DataTypeIndex)
            {
                case 0: CalcValue = CalcEmptyUnsignedValue; break;
                case 1: CalcValue = CalcEmptySignedValue; break;
                case 2: CalcValue = CalcEmptySingleValue; break;
                case 3: CalcValue = CalcEmptyDoubleValue; break;
            }
        }

        public BinaryData(UInt16 StartBit, UInt16 BitCount, bool isIntel, int DataTypeIndex, double Factor, double Offset) : this(StartBit, BitCount, isIntel, DataTypeIndex)
        {
            this.Factor = Factor;
            this.Offset = Offset;

            switch (DataTypeIndex)
            {
                case 0: CalcValue = CalcFxUnsignedValue; break;
                case 1: CalcValue = CalcFxSignedValue; break;
                case 2: CalcValue = CalcFxSingleValue; break;
                case 3: CalcValue = CalcFxDoubleValue; break;
            }
        }

        public BinaryData(UInt16 StartBit, UInt16 BitCount, bool isIntel, int DataTypeIndex, TableNumericConversion Table) : this(StartBit, BitCount, isIntel, DataTypeIndex)
        {
            this.Table = Table;

            switch (DataTypeIndex)
            {
                case 0: CalcValue = CalcTableUnsignedValue; break;
                case 1: CalcValue = CalcTableSignedValue; break;
                case 2: CalcValue = CalcTableSingleValue; break;
                case 3: CalcValue = CalcTableDoubleValue; break;
            }
        }

        public BinaryData(UInt16 StartBit, UInt16 BitCount, bool isIntel, int DataTypeIndex, double Factor, double Offset, TableNumericConversion Table) : this(StartBit, BitCount, isIntel, DataTypeIndex)
        {
            this.Factor = Factor;
            this.Offset = Offset;
            this.Table = Table;

            switch (DataTypeIndex)
            {
                case 0: CalcValue = CalcTableFxUnsignedValue; break;
                case 1: CalcValue = CalcTableFxSignedValue; break;
                case 2: CalcValue = CalcTableFxSingleValue; break;
                case 3: CalcValue = CalcTableFxDoubleValue; break;
            }
        }

        public static UInt64 ByteReadIntel(byte[] arr, int Offset, int ByteCount)
        {
            try
            {
                UInt64 data = 0;
                for (int hp = 0; hp < ByteCount; hp++)
                    data |= (UInt64)arr[Offset + hp] << BytePos[hp];

                return data;
            }
            catch (Exception e)
            {

                return 0;
            }
            
        }

        public static UInt64 ByteReadMotorola(byte[] arr, int Offset, int ByteCount)
        {
            UInt64 data = 0;
            for (int hp = 0, tp = ByteCount - 1; hp < ByteCount; hp++, tp--)
                data |= (UInt64)arr[Offset + hp] << BytePos[tp];

            return data;
        }

        public bool ExtractHex(byte[] HexMessage, out HexStruct hex)
        {

            hex = new HexStruct() { };

            // Check if data exist
            if (byteOffset + canBytes > HexMessage.Length || canBytes > 8)
                return false;

            // Extract raw data
            hex.Unsigned = ByteRead(HexMessage, byteOffset, canBytes);
            hex.Unsigned = (hex.Unsigned >> bitOffset) & bitMask;

            // Fix sign
            if (HexType == typeof(Int64))
                if ((hex.Unsigned & signBitmask) == signBitmask)
                    hex.Unsigned |= ~bitMask;

            return true;
        }

        double CalcEmptyUnsignedValue(ref HexStruct hex) => hex.Unsigned;
        double CalcEmptySignedValue(ref HexStruct hex) => hex.Signed;
        double CalcEmptySingleValue(ref HexStruct hex) => hex.Single;
        double CalcEmptyDoubleValue(ref HexStruct hex) => hex.Double;
        double CalcFxUnsignedValue(ref HexStruct hex) => hex.Unsigned * Factor + Offset;
        double CalcFxSignedValue(ref HexStruct hex) => hex.Signed * Factor + Offset;
        double CalcFxSingleValue(ref HexStruct hex) => hex.Single * Factor + Offset;
        double CalcFxDoubleValue(ref HexStruct hex) => hex.Double * Factor + Offset;
        double CalcTableUnsignedValue(ref HexStruct hex) => Table.Interpolate(hex.Unsigned);
        double CalcTableSignedValue(ref HexStruct hex) => Table.Interpolate(hex.Signed);
        double CalcTableSingleValue(ref HexStruct hex) => Table.Interpolate(hex.Single);
        double CalcTableDoubleValue(ref HexStruct hex) => Table.Interpolate(hex.Double);
        double CalcTableFxUnsignedValue(ref HexStruct hex) => Table.Interpolate(CalcFxUnsignedValue(ref hex));
        double CalcTableFxSignedValue(ref HexStruct hex) => Table.Interpolate(CalcFxSignedValue(ref hex));
        double CalcTableFxSingleValue(ref HexStruct hex) => Table.Interpolate(CalcFxSingleValue(ref hex));
        double CalcTableFxDoubleValue(ref HexStruct hex) => Table.Interpolate(CalcFxDoubleValue(ref hex));

    }
}
