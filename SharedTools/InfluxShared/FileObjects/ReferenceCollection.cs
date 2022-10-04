using System;
using System.Collections.Generic;
using System.Linq;

namespace InfluxShared.FileObjects
{
    public class ReferenceChannel
    {
        public byte BusChannelIndex { get; set; }

        public string FileName { get; set; }
    }

    public class ReferenceDbcChannel : ReferenceChannel
    {
        public UInt32 MessageID { get; set; }

        public string SignalName { get; set; }

        public static bool operator ==(ReferenceDbcChannel item1, ReferenceDbcChannel item2) =>
            item1.BusChannelIndex == item2.BusChannelIndex &&
            item1.FileName == item2.FileName &&
            item1.MessageID == item2.MessageID &&
            item1.SignalName == item2.SignalName;
        public static bool operator !=(ReferenceDbcChannel item1, ReferenceDbcChannel item2) => !(item1 == item2);
    }

    public class ReferenceLdfChannel : ReferenceChannel
    {
        public byte MessageID { get; set; }

        public string SignalName { get; set; }

        public static bool operator ==(ReferenceLdfChannel item1, ReferenceLdfChannel item2) =>
            item1.BusChannelIndex == item2.BusChannelIndex &&
            item1.FileName == item2.FileName &&
            item1.MessageID == item2.MessageID &&
            item1.SignalName == item2.SignalName;
        public static bool operator !=(ReferenceLdfChannel item1, ReferenceLdfChannel item2) => !(item1 == item2);
    }

    public class ExportCollections
    {
        public ExportDbcCollection dbcCollection;
        public ExportLdfCollection ldfCollection;

        public ExportCollections()
        {
            dbcCollection = new ExportDbcCollection();
            ldfCollection = new ExportLdfCollection();
        }
    }

    public class ReferenceCollection : List<ReferenceChannel>
    {
        internal readonly ObjectLibrary ObjLibrary;

        public ReferenceCollection(ObjectLibrary AObjLibrary)
        {
            ObjLibrary = AObjLibrary;
        }

        public void Add(ReferenceChannel channel)
        {
            if (this.Where(c => c.GetType() == channel.GetType()).FirstOrDefault(c => c == channel) != null)
                return;

            base.Add(channel);
        }

        public ExportCollections GetExportCollections()
        {
            ExportCollections config = new ExportCollections();

            foreach (var channel in this)
                if (channel is ReferenceDbcChannel)
                {
                    var dbc = ObjLibrary.DBCFiles.FirstOrDefault(d => d.FileNameSerialized == channel.FileName);
                    if (dbc is null)
                        continue;
                    var msg = dbc.Messages.FirstOrDefault(m => m.FullID == (channel as ReferenceDbcChannel).MessageID);
                    if (msg is null)
                        continue;
                    var sig = msg.Items.FirstOrDefault(s => s.Name == (channel as ReferenceDbcChannel).SignalName);
                    if (sig is null)
                        break;

                    config.dbcCollection.AddMessage(channel.BusChannelIndex, msg).AddSignal(sig);
                }
                else if (channel is ReferenceLdfChannel)
                {
                    var ldf = ObjLibrary.LDFFiles.FirstOrDefault(d => d.FileNameSerialized == channel.FileName);
                    if (ldf is null)
                        continue;
                    var msg = ldf.Messages.FirstOrDefault(m => m.ID == (channel as ReferenceLdfChannel).MessageID);
                    if (msg is null)
                        continue;
                    var sig = msg.Items.FirstOrDefault(s => s.Name == (channel as ReferenceLdfChannel).SignalName);
                    if (sig is null)
                        break;
                    
                    config.ldfCollection.AddMessage(channel.BusChannelIndex, msg).AddSignal(sig);
                }

            return config;
        }

        public List<DBC> GetAssignedDbc()
        {
            var dbclist = new List<DBC>();
            foreach (var channel in this.OfType<ReferenceDbcChannel>())
            {
                var dbc = ObjLibrary.DBCFiles.FirstOrDefault(d => d.FileNameSerialized == channel.FileName);
                if (dbc is null)
                    continue;
                if (!dbclist.Contains(dbc))
                    dbclist.Add(dbc);
            }
            return dbclist;
        }

        public List<LDF> GetAssignedLdf()
        {
            var ldflist = new List<LDF>();
            foreach (var channel in this.OfType<ReferenceLdfChannel>())
            {
                var ldf = ObjLibrary.LDFFiles.FirstOrDefault(d => d.FileNameSerialized == channel.FileName);
                if (ldf is null)
                    continue;
                if (!ldflist.Contains(ldf))
                    ldflist.Add(ldf);
            }
            return ldflist;
        }

        public bool IsInUse(object obj)
        {
            if (obj is DBC)
            {
                foreach (var item in this)
                    if (item.FileName == (obj as DBC).FileNameSerialized)
                        return true;
            }
            else if (obj is LDF)
            {
                foreach (var item in this)
                    if (item.FileName == (obj as LDF).FileNameSerialized)
                        return true;
            }

            return false;
        }
    }
}
