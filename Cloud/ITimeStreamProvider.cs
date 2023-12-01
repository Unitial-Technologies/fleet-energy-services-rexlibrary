using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cloud
{
    public interface ITimeStreamProvider
    {
        public Task<bool> ToTimeStream(DoubleDataCollection ddc, string filename);
        public Task<bool> WriteSnapshot(string device_id, string json);
    }
}
