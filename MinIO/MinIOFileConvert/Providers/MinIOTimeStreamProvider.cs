using Cloud;
using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinIOFileConvert.Providers
{
    internal class MinIOTimeStreamProvider : ITimeStreamProvider
    {
        public Task<bool> ToTimeStream(DoubleDataCollection ddc, string filename)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteSnapshot(string device_id, string json)
        {
            throw new NotImplementedException();
        }
    }
}
