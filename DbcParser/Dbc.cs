using DbcParserLib.Model;
using System.Collections.Generic;
using InfluxShared.FileObjects;

namespace DbcParserLib
{
    public class Dbc
    {
        public IEnumerable<Node> Nodes { get; }
        public IEnumerable<Message> Messages { get; }
        public DBCFileType FileType { get; set; }

        public Dbc(IEnumerable<Node> nodes, IEnumerable<Message> messages)
        {
            Nodes = nodes;
            Messages = messages;
        }
    }
}