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

        // Precalculated variables
        readonly UInt16 byteOffset;
        readonly UInt16 bitOffset;
        readonly UInt64 bitMask;
        readonly UInt16 canBytes;
        readonly UInt64 signBitmask;

        delegate UInt64 ByteReadMethod(byte[] arr, int Offset, int ByteCount);
        readonly ByteReadMethod ByteRead;
        public delegate double CalcValueMethod(ref HexStruct hex);
        public readonly CalcValueMethod CalcValue;

        public BinaryData(UInt16 StartBit, UInt16 BitCount, bool isIntel, int DataTypeIndex, double Factor, double Offset)
        {
            this.StartBit = StartBit;
            this.BitCount = BitCount;
            this.isIntel = isIntel;
            this.HexType = BinaryTypes[DataTypeIndex];
            this.Factor = Factor;
            this.Offset = Offset;

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
                case 0: CalcValue = CalcUnsignedValue; break;
                case 1: CalcValue = CalcSignedValue; break;
                case 2: CalcValue = CalcSingleValue; break;
                case 3: CalcValue = CalcDoubleValue; break;
            }
        }

        public static UInt64 ByteReadIntel(byte[] arr, int Offset, int ByteCount)
        {
            UInt64 data = 0;
            for (int hp = 0; hp < ByteCount; hp++)
                data |= (UInt64)arr[Offset + hp] << BytePos[hp];

            return data;
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
            if (byteOffset + canBytes > HexMessage.Length)
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

        double CalcUnsignedValue(ref HexStruct hex) => hex.Unsigned * Factor + Offset;
        double CalcSignedValue(ref HexStruct hex) => hex.Signed * Factor + Offset;
        double CalcSingleValue(ref HexStruct hex) => hex.Single * Factor + Offset;
        double CalcDoubleValue(ref HexStruct hex) => hex.Double * Factor + Offset;

    }
}
