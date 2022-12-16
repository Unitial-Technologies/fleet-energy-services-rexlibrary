using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class FieldConfig
        {
            public Defaults defaults { get; set; }
            public List<object> overrides { get; set; }

            public FieldConfig()
            {
                defaults = new Defaults();
                overrides = new List<object>();
            }
        }
    }  

    
    
}
