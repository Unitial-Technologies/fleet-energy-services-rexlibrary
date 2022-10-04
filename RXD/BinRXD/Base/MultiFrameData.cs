using RXD.DataRecords;
using System;

namespace RXD.Base
{
    internal class MultiFrameData : RecordCollection
    {
        public UInt32 PGN;
        public UInt32 Source;
        public UInt16 MessageSize;
        public byte FrameCount;

        public bool isCompleted => Count == FrameCount + 1;

        public RecCanTrace PackJ1939Message()
        {
            RecCanTrace rec = new RecCanTrace
            {
                header = new RecHeader()
                {
                    UID = this[0].header.UID,
                    InfSize = this[0].header.InfSize,
                    DLC = (byte)MessageSize
                },
                LinkedBin = this[0].LinkedBin,
                BusChannel = this[0].BusChannel,
                NotExportable = true,
            };

            rec.data.Timestamp = (this[FrameCount] as RecCanTrace).data.Timestamp;
            rec.data.Flags = (this[0] as RecCanTrace).data.Flags;
            rec.data.CanID = (this[0] as RecCanTrace).data.CanID;
            rec.data.CanID.PGN = PGN;

            rec.VariableData = new byte[rec.header.DLC];

            for (int i = 1; i <= FrameCount; i++)
                Array.Copy(this[i].VariableData, 1, rec.VariableData, (i - 1) * 7, Math.Min(7, MessageSize - (i - 1) * 7));

            return rec;
        }
    }
}
