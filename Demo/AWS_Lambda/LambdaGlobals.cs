using Amazon.Lambda.Core;
using Amazon.S3;

namespace AWSLambdaFileConvert
{
    public static class LambdaGlobals
    {        
        internal static S3Storage S3 { get; set; }
        public static IAmazonS3 S3Client { get;set; }
        public static ILambdaContext Context { get; set; }
        internal static string Bucket { get; set; }
        internal static string OutputBucket { get; set; } = "";
        internal static string FilePath { get; set; } = "";
        internal static string FileName { get; set; } = "";
        internal static string LoggerDir { get; set; } = "";
        internal static bool DeleteInputFile { get; set; } = false;
        
    }
}
