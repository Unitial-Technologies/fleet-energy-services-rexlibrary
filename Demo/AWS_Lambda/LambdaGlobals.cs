using Amazon.Lambda.Core;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaFileConvert
{
    public static class LambdaGlobals
    {        
        internal static S3Storage S3 { get; set; }
        internal static IAmazonS3 S3Client { get;set; }
        internal static ILambdaContext Context { get; set; }
        internal static string Bucket { get; set; }
        internal static string FilePath { get; set; } = "";
        internal static string FileName { get; set; } = "";
        internal static string LoggerDir { get; set; } = "";
        internal static dynamic ConfigJson { get; set; }
        internal static TimestreamSettings Timestream { get; set; }
        internal static SnapshotSettings Snapshot { get; set; }
        internal static InfluxDBSettings InfluxDB { get; set; }
        internal class TimestreamSettings
        {
            public bool enabled { get; set; }
            public string db_name { get; set; }
            public string table_name { get; set; }
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
    }
}
