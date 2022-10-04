using RXD.DataRecords;
using SharedObjects;
using System;
using System.Collections.Generic;

namespace RXD.Base
{
    internal class MultiFrameCollection : Dictionary<UInt32, MultiFrameData>
    {
        public MultiFrameCollection()
        {

        }

        public MultiFrameData GetJ1939(CanIdentifier ident) => TryGetValue(ident.Source, out MultiFrameData data) ? data : null;

        public MultiFrameData AddJ1939(RecCanTrace msg)
        {
            MultiFrameData data = new MultiFrameData()
            {
                PGN = (UInt32)(msg.VariableData[5] | msg.VariableData[6] << 8 | msg.VariableData[7] << 16),
                Source = msg.data.CanID.Source,
                MessageSize = BitConverter.ToUInt16(msg.VariableData, 1),
                FrameCount = msg.VariableData[3]
            };

            base.Add(data.Source, data);

            return data;
        }

        public MultiFrameData AddOrGetJ1939(RecCanTrace msg)
        {
            MultiFrameData data;
            if (TryGetValue(msg.data.CanID.Source, out data))
                return data;

            data = new MultiFrameData()
            {
                PGN = (UInt32)(msg.VariableData[5] | msg.VariableData[6] << 8 | msg.VariableData[7] << 16),
                Source = msg.data.CanID.Source,
                MessageSize = BitConverter.ToUInt16(msg.VariableData, 1),
                FrameCount = msg.VariableData[3]
            };

            base.Add(data.Source, data);

            return data;
        }

    }
}
