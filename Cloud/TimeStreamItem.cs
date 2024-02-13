using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Cloud
{
    public class PointD
    {
        public long Timestamp { get; set; }
        public double Value { get; set; }
    }
    public class TimeStreamItem
    {
        public string DeviceId { get; set; } = "";
        public string Filename { get; set; } = "";
        public string Bus { get; set; } = "";
        public string Name { get; set; } = "";
        public List<PointD> Points { get; set; } = new List<PointD>();
    }
}
