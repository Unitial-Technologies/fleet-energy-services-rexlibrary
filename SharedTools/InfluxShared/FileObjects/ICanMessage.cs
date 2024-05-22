using System.Collections.Generic;

namespace InfluxShared.FileObjects
{
    public interface ICanMessage
    {
        public string Name { get; set; }
        public uint CANID { get; set; }
        public byte DLC { get; set; }
        public DBCMessageType MsgType { get; set; }
        public string Transmitter { get; set; }
        public string Comment { get; set; }
        public List<ICanSignal> Signals { get; }
        public bool EqualProps(object obj);
        public bool Log { get; set; }
    }

}
