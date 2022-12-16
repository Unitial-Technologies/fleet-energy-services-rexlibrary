using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Annotations
        {
            public List<List> list { get; set; }

            public Annotations()
            {
                list = new List<List>();
            }
        }
    }  

    
    
}
