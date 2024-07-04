using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MatlabFile.Data
{
    public static class MType
    {
        public static Dictionary<MElementType, Type> ElementType = new Dictionary<MElementType, Type>()
        {
            { MElementType.INT8, typeof(byte) },
            { MElementType.UINT8, typeof(sbyte) },
            { MElementType.INT16, typeof(Int16) },
            { MElementType.UINT16, typeof(UInt16) },
            { MElementType.INT32, typeof(Int32) },
            { MElementType.UINT32, typeof(UInt32) },
            { MElementType.Single, typeof(Single) },
            { MElementType.Double, typeof(Double) },
            { MElementType.INT64, typeof(Int64) },
            { MElementType.UINT64, typeof(UInt64) },
        };

        public static Dictionary<MMatrixType, MElementType> MatrixSubType = new Dictionary<MMatrixType, MElementType>()
        {
            { MMatrixType.CharacterClass, MElementType.UINT8 },
            { MMatrixType.DoubleArray, MElementType.Double },
            { MMatrixType.SingleArray, MElementType.Single },
            { MMatrixType.Int8Class, MElementType.INT8 },
            { MMatrixType.Uint8Class, MElementType.UINT8 },
            { MMatrixType.Int16Class, MElementType.INT16 },
            { MMatrixType.Uint16Class, MElementType.UINT16 },
            { MMatrixType.Int32Class, MElementType.INT32 },
            { MMatrixType.Uint32Class, MElementType.UINT32 },
            { MMatrixType.Int64Class, MElementType.INT64 },
            { MMatrixType.Uint64Class, MElementType.UINT64 },
        };

        public static int SizeOf(MElementType MType) => Marshal.SizeOf(ElementType[MType]);
    }
}
