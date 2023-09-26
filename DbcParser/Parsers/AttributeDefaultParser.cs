using System;
using System.Collections.Generic;
using System.Text;

namespace DbcParser.Parsers
{
    public static class AttributeDefaultParser
    {
     
        public static List<uint> ID_List = new List<uint>();
        public static Dictionary<string,string> AttrTypes = new Dictionary<string, string>();
        public static bool isJ1939 = false;
    }
}
