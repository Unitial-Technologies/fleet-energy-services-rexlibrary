namespace Cloud.Grafana
{
    public partial class Dashboard
    {
        public class Custom
        {
            public bool axisCenteredZero { get; set; }
            public string axisColorMode { get; set; }
            public string axisLabel { get; set; }
            public string axisPlacement { get; set; }
            public int barAlignment { get; set; }
            public string drawStyle { get; set; }
            public int fillOpacity { get; set; }
            public string gradientMode { get; set; }
            public HideFrom hideFrom { get; set; }
            public string lineInterpolation { get; set; }
            public int lineWidth { get; set; }
            public int pointSize { get; set; }
            public ScaleDistribution scaleDistribution { get; set; }
            public string showPoints { get; set; }
            public bool spanNulls { get; set; }
            public Stacking stacking { get; set; }
            public ThresholdsStyle thresholdsStyle { get; set; }

            public Custom()
            {
                hideFrom = new HideFrom();
                scaleDistribution = new ScaleDistribution();
                stacking = new Stacking();
                thresholdsStyle = new ThresholdsStyle();
            }
        }
    }  

    
    
}
