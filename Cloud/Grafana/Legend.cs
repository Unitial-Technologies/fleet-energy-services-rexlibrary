using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Legend
        {
            public List<object> calcs { get; set; }
            public string displayMode { get; set; }
            public string placement { get; set; }
            public bool showLegend { get; set; }

            public Legend()
            {
                calcs = new List<object>();
            }
        }
    }  

    
    
}
