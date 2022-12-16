using RXD.DataRecords;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RXD.Base
{
    internal class PreBufferCollection : List<RecPreBuffer>
    {
        public bool IncludeLastUntriggered = true;

        public PreBufferCollection()
        {

        }

        //public UInt32 GetFileTimestamp => Count == 0 ? 0 : this[0].data.InitialTimestamp;

        public List<(UInt32, UInt32)> GetSectorMap()
        {
            var PreBufferList = this.Where(x => x.data.ContainPreBufferInfo).ToList();

            /*var LastBuffer = PreBufferList.LastOrDefault();
            if (LastBuffer is not null)
                if (LastBuffer.data.Timestamp == 0)
                    IncludeLastUntriggered = false;*/

            List<(UInt32, UInt32)> map = new List<(UInt32, UInt32)>();

            // [PreCurrentSector..PreEndSector], [PreStartSector..PreCurrentSector)
            foreach (var pb in PreBufferList)
            {
                if (pb.data.ContainPreBufferInfo)
                {
                    if (IncludeLastUntriggered || pb.data.ContainPostData)
                    {
                        // If overwritten buffer available point to current sector + 1
                        if (pb.data.PreCurrentSector < pb.data.PreEndSector)
                            map.Add((pb.data.PreStartSector - 1, pb.data.PreCurrentSector + 1));

                        // Go to start sector
                        map.Add((pb.data.PreStartSector - 1 + pb.data.PreEndSector - pb.data.PreCurrentSector, pb.data.PreStartSector));
                        // Go to event info
                        map.Add((pb.data.PreEndSector - (UInt32)(pb.data.Timestamp == 0 ? 1 : 0), pb.data.PreStartSector - 1));
                        // Go to post log
                        map.Add((pb.data.PreEndSector + 1 - (UInt32)(pb.data.Timestamp == 0 ? 1 : 0), pb.data.PreEndSector + 1));
                    }
                    else
                    {
                        map.Add((pb.data.PreStartSector - 1, UInt32.MaxValue));
                    }
                }
            }

            return map;
        }

    }
}
