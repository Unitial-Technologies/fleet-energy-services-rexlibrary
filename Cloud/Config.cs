using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cloud
{
    [Flags]
    public enum ConversionType
    {
        None = 0,
        Csv = 1,
        TimeStream = 2,
        InfluxDB = 4,
        Snapshot = 8,
        Rxc = 16,
        Mdf = 32,
        Blf = 64,
    }
    internal static class Config
    {
        internal static dynamic ConfigJson { get; set; }
        internal static TimestreamSettings Timestream { get; set; }
        internal static SnapshotSettings Snapshot { get; set; }
        internal static InfluxDBSettings InfluxDB { get; set; }
        internal class TimestreamSettings
        {
            public bool enabled { get; set; }
            public string db_name { get; set; }
            public string table_name { get; set; }
            public bool downsampling { get; set; } = false;
            public double downsampling_tolerance { get; set; } = 0.1;
        }
        internal class SnapshotSettings
        {
            public bool enabled { get; set; }
            public string db_name { get; set; }
            public string table_name { get; set; }
        }

        internal class InfluxDBSettings
        {
            public bool enabled { get; set; }
            public string url { get; set; }
            public string token { get; set; }
            public string org { get; set; }
            public string bucket { get; set; }
        }

        internal static bool LoadSettings(Stream jsonConfig)
        {
            try
            {
                using (StreamReader reader = new(jsonConfig))
                    ConfigJson = JsonConvert.DeserializeObject(reader.ReadToEnd());
                Timestream = ConfigJson.AmazonTimestream.ToObject<TimestreamSettings>();
                if (Timestream.db_name == "default")
                    Timestream.db_name = "rxd";
                if (Timestream.table_name == "default")
                    Timestream.table_name = "rxddata";

                Snapshot = ConfigJson.Snapshot.ToObject<SnapshotSettings>();
                if (Snapshot.db_name == "default")
                    Snapshot.db_name = "snapshot";
                if (Snapshot.table_name == "default")
                    Snapshot.table_name = "snapshot";

                InfluxDB = ConfigJson.InfluxDB.ToObject<InfluxDBSettings>();
                string vars = "";
                var vardict = Environment.GetEnvironmentVariables();
                foreach (var item in vardict.Keys)
                {
                    vars += $"Key:{item} ";
                }
                //InfluxDB.token = Environment.GetEnvironmentVariable("influxdb_token");
                if (InfluxDB.token is null)
                {
                    InfluxDB.token = "";
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        internal static ConversionType GetConversions()
        {
            ConversionType convert = 0;
            if (Config.ConfigJson.ContainsKey("InfluxDB") && Config.ConfigJson.InfluxDB.ContainsKey("enabled") && (Config.ConfigJson.InfluxDB.enabled == true))
                convert |= Cloud.ConversionType.InfluxDB;
            if (Config.ConfigJson.ContainsKey("CSV") && Config.ConfigJson.CSV.ContainsKey("enabled") && (Config.ConfigJson.CSV.enabled == true))
                convert |= Cloud.ConversionType.Csv;
            if (Config.ConfigJson.ContainsKey("AmazonTimestream") && Config.ConfigJson.AmazonTimestream.ContainsKey("enabled") && (Config.ConfigJson.AmazonTimestream.enabled == true))
                convert |= Cloud.ConversionType.TimeStream;
            if (Config.ConfigJson.ContainsKey("Snapshot") && Config.ConfigJson.Snapshot.ContainsKey("enabled") && (Config.ConfigJson.Snapshot.enabled == true))
                convert |= Cloud.ConversionType.Snapshot;
            if (Config.ConfigJson.ContainsKey("MDF") && Config.ConfigJson.MDF.ContainsKey("enabled") && (Config.ConfigJson.MDF.enabled == true))
                convert |= Cloud.ConversionType.Mdf;
            if (Config.ConfigJson.ContainsKey("BLF") && Config.ConfigJson.BLF.ContainsKey("enabled") && (Config.ConfigJson.BLF.enabled == true))
                convert |= Cloud.ConversionType.Blf;
            return convert;
        }
    }
}
