using System.Collections.Generic;

namespace DbcParser.Parsers
{
    public class AttributeDefaultParser
    {

        public List<uint> ID_List = new List<uint>();
        public Dictionary<string, string> AttrTypes = new Dictionary<string, string>();
        public bool isJ1939 = false;
    }
}
