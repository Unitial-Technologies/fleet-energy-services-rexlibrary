namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class List
        {
            public int builtIn { get; set; }
            public Datasource datasource { get; set; }
            public bool enable { get; set; }
            public bool hide { get; set; }
            public string iconColor { get; set; }
            public string name { get; set; }
            public Target target { get; set; }
            public string type { get; set; }

            public List()
            {
                datasource = new Datasource();
                target = new Target();
            }
        }
    }  

    
    
}
