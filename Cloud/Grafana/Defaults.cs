using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Defaults
        {
            public Color color { get; set; }
            public Custom custom { get; set; }
            public List<object> mappings { get; set; }
            public Thresholds thresholds { get; set; }

            public Defaults()
            {
                mappings = new List<object>();
                thresholds = new Thresholds();
                color = new Color();
                custom = new Custom();
            }
        }
    }  

    
    
}
