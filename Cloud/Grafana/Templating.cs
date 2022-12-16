using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Templating
        {
            public List<object> list { get; set; }

            public Templating()
            {
                list = new List<object>();
            }
        }
    }  

    
    
}
