using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public Annotations annotations { get; set; }
        public bool editable { get; set; }
        public int fiscalYearStartMonth { get; set; }
        public int graphTooltip { get; set; }
        public int? id { get; set; }
        public List<object> links { get; set; }
        public bool liveNow { get; set; }
        public List<Panel> panels { get; set; }
        public bool refresh { get; set; }
        public int schemaVersion { get; set; }
        public string style { get; set; }
        public List<string> tags { get; set; }
        public Templating templating { get; set; }
        public Time time { get; set; }
        public Timepicker timepicker { get; set; }
        public string timezone { get; set; }
        public string title { get; set; }
        public string uid { get; set; }
        public int version { get; set; }
        public string weekStart { get; set; }

        public Dashboard()
        {
            tags = new List<string>();
            timepicker = new Timepicker();
            time = new Time();
            templating = new Templating();
            panels = new List<Panel>();
            links = new List<object>();
            annotations = new Annotations();
        }
    }  

    
    
}
