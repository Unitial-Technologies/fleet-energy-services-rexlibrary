using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Panel
        {
            public Datasource datasource { get; set; }
            public FieldConfig fieldConfig { get; set; }
            public GridPos gridPos { get; set; }
            public int id { get; set; }
            public Options options { get; set; }
            public List<Target> targets { get; set; }
            public string title { get; set; }
            public string type { get; set; }
        }
    }  

    
    
}
