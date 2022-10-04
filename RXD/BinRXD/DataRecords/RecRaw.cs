using InfluxShared.Generic;
using InfluxShared.Helpers;
using RXD.Blocks;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RXD.DataRecords
{
    public class RecRaw
    {
        internal RecHeader header;
        internal byte[] Data;
        internal byte[] VariableData;

        internal byte BusChannel;
        internal BinBase LinkedBin = null;

        public RecRaw()
        {

        }

        public override string ToString()
        {
            return $"UID: {header.UID}; Infsize: {header.InfSize}; DLC: {header.DLC}; Timestamp: " + BitConverter.ToUInt32(Data, 0) + ";   Data: " + BitConverter.ToString(Data) + ";   VariableData: " + BitConverter.ToString(VariableData) + "\r\n";
        }

        public static RecRaw Read(ref IntPtr src)
        {
            RecRaw rec = new RecRaw();

            rec.header = new RecHeader();
            Marshal.PtrToStructure(src, rec.header);
            src += Marshal.SizeOf(rec.header);

            rec.Data = new byte[rec.header.InfSize];
            Marshal.Copy(src, rec.Data, 0, rec.header.InfSize);
            src += rec.header.InfSize;

            rec.VariableData = new byte[rec.header.DLC];
            Marshal.Copy(src, rec.VariableData, 0, rec.header.DLC);
            src += rec.header.DLC;

            return rec;
        }

        public static void ApplyTimestampOffset(ref IntPtr src, Int64 Offset)
        {
            RecHeader hdr = new RecHeader();
            Marshal.PtrToStructure(src, hdr);
            src += Marshal.SizeOf(hdr);

            UInt32 tmpTime = (UInt32)(Marshal.PtrToStructure<UInt32>(src) + Offset);
            Marshal.StructureToPtr(tmpTime, src, false);

            src += hdr.InfSize + hdr.DLC;
        }

        public static RecRaw Read(BinaryReader br)
        {
            RecRaw rec = new RecRaw();

            rec.header = br.ReadBytes(Marshal.SizeOf(typeof(RecHeader))).ConvertTo<RecHeader>();
            rec.Data = br.ReadBytes(rec.header.InfSize);
            rec.VariableData = br.ReadBytes(rec.header.DLC);

            return rec;
        }
    }
}
