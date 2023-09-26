using System.Collections.Specialized;

namespace DbcParserLib.Model
{
    public class Node
    {
        public string Name;
        public string Comment;
        public NameValueCollection AttrValues = new NameValueCollection();
    }
}