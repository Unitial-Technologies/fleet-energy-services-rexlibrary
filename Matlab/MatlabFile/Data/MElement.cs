using InfluxShared.Generic;
using InfluxShared.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MatlabFile.Data
{
    public enum MElementType : UInt16
    {
        INT8 = 0x0001,
        UINT8 = 0x0002,
        INT16 = 0x0003,
        UINT16 = 0x0004,
        INT32 = 0x0005,
        UINT32 = 0x0006,
        Single = 0x0007,
        Double = 0x0009,
        INT64 = 0x000C,
        UINT64 = 0x000D,
        Matrix = 0x000E,
        COMPRESSED = 0x000F,
        UTF8 = 0x0010,
        UTF16 = 0x0011,
        UTF32 = 0x0012,
    }

    public enum MMatrixType : UInt32
    {
        CellClass = 0x00000001,
        StructureClass = 0x00000002,
        ObjectClass = 0x00000003,
        CharacterClass = 0x00000004,
        SparseClass = 0x00000005,
        DoubleArray = 0x00000006,
        SingleArray = 0x00000007,
        Int8Class = 0x00000008,
        Uint8Class = 0x00000009,
        Int16Class = 0x0000000A,
        Uint16Class = 0x0000000B,
        Int32Class = 0x0000000C,
        Uint32Class = 0x0000000D,
        Int64Class = 0x0000000E,
        Uint64Class = 0x0000000F,
    }

    public class MElement
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        internal class MHeader
        {
            internal MElementType Type;
            UInt16 reserved;
            UInt32 DataBytes;

            //internal bool SmallFormat => SmallElementCheck != 0;

            //internal byte[] SmallData => BitConverter.GetBytes(DataBytes);

            internal UInt32 DataSize
            {
                //get => SmallFormat ? SmallElementCheck : DataBytes;
                get => DataBytes;
                set
                {
                    /*if (value <= 4)
                        SmallElementCheck = (UInt16)value;
                    else*/
                    {
                        DataBytes = value;
                        reserved = 0;
                    }
                }
            }
        }

        internal MHeader header;

        internal MElementType Type
        {
            get => header.Type;
            set => header.Type = value;
        }

        UInt32 ElementCount = 0;

        internal UInt32 DataLength
        {
            get => ElementCount;
            set
            {
                ElementCount = value;
                header.DataSize = DataSize;
            }
        }

        internal UInt32 DataSize => (Type == MElementType.Matrix) ? (UInt32)Childs.Sum(c => c.BlockSize) : (UInt32)Align(ElementCount * MType.SizeOf(header.Type));
        internal UInt32 BlockSize => (UInt32)Marshal.SizeOf(header) + DataSize;

        internal List<MElement> Childs = new List<MElement>();

        internal byte[] Data;

        internal Int64 FilePos;
        internal Int64 FileEnd;
        internal Int64 DataPos = -1;

        public MElement(MElementType Type)
        {
            header = new MHeader();
            header.Type = Type;
        }

        public MElement(MElementType Type, UInt32 MDataLength)
        {
            header = new MHeader();
            header.Type = Type;
            DataLength = MDataLength;
        }

        public static void Align(ref Int64 value) => value = (value + 7) & ~7;

        public static Int64 Align(Int64 value) => (value + 7) & ~7;

        internal Int64[] DataOffsets
        {
            get
            {
                if (header.Type == MElementType.Matrix)
                {
                    MElement dataEl = Childs[3];
                    if (dataEl.DataPos == -1)
                        return null;

                    MElement dimEl = Childs[1];
                    int samples = dimEl.Data.ReadTo(MType.ElementType[dimEl.Type], 0);
                    int columns = dimEl.Data.ReadTo(MType.ElementType[dimEl.Type], 1);
                    Int64[] offsets = new Int64[columns];
                    for (int i = 0; i < columns; i++)
                        offsets[i] = dataEl.DataPos + i * samples * MType.SizeOf(dataEl.header.Type);

                    return offsets;
                }
                else
                    return DataPos == -1 ? null : new Int64[1] { DataPos };
            }
        }

        public void PrepareOffsets(Int64 AFilePos)
        {
            FilePos = AFilePos;
            FileEnd = AFilePos + BlockSize;

            // Update header datasize field
            header.DataSize = DataSize;

            // Calculate data file position
            Int64 dp = FilePos + Marshal.SizeOf(header);

            if (Type == MElementType.Matrix)
            {
                foreach (var child in Childs)
                {
                    child.PrepareOffsets(dp);
                    dp = child.FileEnd;
                }
            }
            else if (Data is null)
            {
                DataPos = dp;
            }
        }

        public void Write(BinaryWriter bw)
        {
            byte[] buffer = (Data is null) ? Bytes.ObjectToBytes(header) : Bytes.ObjectToBytes(header).Concat(Data).ToArray();
            if (buffer.Length == 0)
                return;

            bw.Seek((int)FilePos, SeekOrigin.Begin);
            bw.Write(buffer);

            foreach (var child in Childs)
                child.Write(bw);
        }
    }
}
