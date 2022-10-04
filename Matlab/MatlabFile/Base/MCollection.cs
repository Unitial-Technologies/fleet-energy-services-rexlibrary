using InfluxShared.Generic;
using MatlabFile.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MatlabFile.Base
{
    public class MCollection : List<MElement>
    {
        public Header header;

        public MCollection()
        {
            header = new Header();
        }

        public void Add(MElement element)
        {
            if (element == null)
                return;

            element.PrepareOffsets(GetFileEnd());
            base.Add(element);
        }

        public Int64 GetFileEnd()
        {
            Int64 fEnd = Marshal.SizeOf(typeof(Header));
            foreach (MElement el in this)
                if (el.FileEnd > fEnd)
                    fEnd = el.FileEnd;

            return fEnd;
        }

        public MElement CreateElement(MElementType ElementType, UInt32 ElementCount)
        {
            MElement el = new MElement(ElementType, ElementCount);
            Add(el);

            return el;
        }

        public MElement CreateElement(MElementType ElementType)
        {
            MElement el = new MElement(ElementType);
            Add(el);

            return el;
        }

        public MElement CreateMatrix2D(string MatrixName, MMatrixType MatrixType, UInt32 Columns, UInt32 SampleCount)
        {
            uint Dimensions = 2;
            MElement el = new MElement(MElementType.Matrix);

            el.Childs = new List<MElement>()
            {
                new MElement(MElementType.UINT32, Dimensions)
                {
                    Data = Bytes.ArrayToBytes(new UInt32[] { (UInt32)MatrixType, 0 })
                },
                new MElement(MElementType.INT32, Dimensions)
                {
                    Data = Bytes.ArrayToBytes(new UInt32[] { SampleCount, Columns })
                },
                new MElement(MElementType.INT8, (uint)MatrixName.Length)
                {
                    Data = Encoding.ASCII.GetBytes(MatrixName)
                },
                new MElement(MType.MatrixSubType[MatrixType], Columns * SampleCount),
                //new MElement(MElement.MatrixSubType[MatrixType], SampleCount),
            };
            Add(el);

            return el;
        }

    }
}
