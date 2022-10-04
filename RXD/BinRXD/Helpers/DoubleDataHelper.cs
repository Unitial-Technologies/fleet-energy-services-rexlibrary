using InfluxShared.FileObjects;
using RXD.Blocks;
using System;

namespace RXD.Helpers
{
    internal static class DoubleDataHelper
    {
        public static DoubleData Object(this DoubleDataCollection ddata, BinBase bin)
        {
            DoubleData data = ddata.GetObject(bin.header.uniqueid);
            if (data is null)
            {
                data = ddata.Add(bin.header.uniqueid, ChannelName: bin.GetName, ChannelUnits: bin.GetUnits);
                data.BinaryHelper = bin.GetDataDescriptor.CreateBinaryData();
            }

            return data;
        }

        public static DoubleData Object(this DoubleDataCollection ddata, BasicItemInfo signal, UInt64 id, byte SourceAddress = 0xFF)
        {
            id |= (UInt64)SourceAddress << 33;
            DoubleData data = ddata.GetObject(id);
            if (data is null)
            {
                ChannelDescriptor ChannelDesc = signal.GetDescriptor;

                data = ddata.Add(id, ChannelName: ChannelDesc.Name, ChannelUnits: ChannelDesc.Units);
                data.BinaryHelper = ChannelDesc.CreateBinaryData();

                if (SourceAddress != 0xFF)
                    data.ChannelName += " [SA: " + SourceAddress.ToString("X2") + "]";
            }
            return data;
        }
    }
}
