using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Thresholds
        {
            public string mode { get; set; }
            public List<Step> steps { get; set; }

            public Thresholds()
            {
                steps = new List<Step>();
            }
        }
    }  

    
    
}
