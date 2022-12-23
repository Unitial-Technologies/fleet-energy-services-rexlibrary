using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaFileConvert.ExportFormats
{
    public class Channel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
       // public dynamic channel { get; set; }
       // public dynamic unit { get; set; }
    }

    public class Feed
    {
        public DateTime time { get; set; }
        public Dictionary<string, double> value { get; set; }
        //public dynamic value { get; set; }

        public Feed()
        {
            value = new Dictionary<string, double>();
        }
    }

    internal class JsonItem
    {        

        public Channel channel { get; set; }
        public List<Feed> feeds { get; set; }

        public JsonItem()
        {
            feeds = new List<Feed>();            
            channel = new Channel();
        }
    }
}
