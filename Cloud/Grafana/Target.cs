using System.Collections.Generic;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Target
        {
            public List<Column> columns { get; set; }
            public Datasource datasource { get; set; }
            public string decimalSeparator { get; set; }
            public string delimiter { get; set; }
            public List<object> filters { get; set; }
            public string format { get; set; }
            public string global_query_id { get; set; }
            public bool header { get; set; }
            public bool ignoreUnknown { get; set; }
            public string refId { get; set; }
            public string root_selector { get; set; }
            public List<Schema> schema { get; set; }
            public int skipRows { get; set; }
            public string source { get; set; }
            public string type { get; set; }
            public string url { get; set; }
            public UrlOptions url_options { get; set; }

            public Target()
            {
                filters = new List<object>();
                schema = new List<Schema>();
                url_options = new UrlOptions();
                datasource = new Datasource();
                columns = new List<Column>();
            }
        }
    }  

    
    
}
