namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Options
        {
            public Legend legend { get; set; }
            public Tooltip tooltip { get; set; }

            public Options()
            {
                legend = new Legend();
                tooltip = new Tooltip();
            }
        }
    }  

    
    
}
