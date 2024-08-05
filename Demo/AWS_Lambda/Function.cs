using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using AWSLambdaFileConvert;
using AWSLambdaFileConvert.ExportFormats;
using AWSLambdaFileConvert.Providers;
using Cloud;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RXD.Base;
using System.Net;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaConvert;


public class Function
{    
    
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        LambdaGlobals.S3Client = new AmazonS3Client();        
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        LambdaGlobals.S3Client = s3Client;
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// 

    /*public async Task<APIGatewayProxyResponse> ConvertFiles(S3Event evnt, ILambdaContext context)
    {        
        var s3Event = evnt.Records?[0].S3;
        if (s3Event == null)
            return null;
        this.Context = context;
        FilePath = Path.GetDirectoryName(s3Event.Object.Key);
        FileName = s3Event.Object.Key.Replace("+", " ");
        if (FilePath is null)
            FilePath = "";
        S3 = new S3Storage(S3Client, context);
        Context?.Logger.LogInformation("GetObjectMetadataAsync: " + s3Event.Bucket.Name +","+ s3Event.Object.Key);

        await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, FileName);

        Context?.Logger.LogInformation("Processing file : " + FileName);

        if (FileName.Contains("Configuration.xml"))
            await ConvertXMLToRxc(s3Event.Bucket.Name, s3Event.Object.Key);
        else if (FileName.Contains(".rxd"))
            await ConvertRXD(s3Event.Bucket.Name, s3Event.Object.Key);
        else if (FileName.Contains(".json"))
            await WriteSnapshot(s3Event.Bucket.Name);

        var rsp = new
        {            
            message = "Hello API",
        };

        var response = new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonConvert.SerializeObject(rsp),
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };
        return response;
    }*/

    Cloud.ConversionType StringToConversionType(string conversion)
    {
        if (conversion.ToLower() == "snapshot")
            return Cloud.ConversionType.Snapshot;
        else if (conversion.ToLower() == "csv")
            return Cloud.ConversionType.Csv;
        else if (conversion.ToLower() == "amazontimestream")
            return Cloud.ConversionType.TimeStream;
        else if (conversion.ToLower() == "influxdb")
            return Cloud.ConversionType.InfluxDB;
        else if (conversion.ToLower() == "rxc")
            return Cloud.ConversionType.Rxc;
        else if (conversion.ToLower() == "mdf")
            return Cloud.ConversionType.Mdf;
        else if (conversion.ToLower() == "blf")
            return Cloud.ConversionType.Blf;
        else
            return Cloud.ConversionType.None;
    }

    async Task<bool> GetConversionJson(string bucket, ILambdaContext context = null)
    {
        var jsonStream = await LambdaGlobals.S3.GetStream(bucket, "FileConvert.json");  
        bool res = Config.LoadSettings(jsonStream);
        
        if (Config.InfluxDB.bucket == "default")
            Config.InfluxDB.bucket = bucket;
        return res;
    }

    //By using Stream as a parameter the function can be triggered by both S3 Event and Http request
    public async Task<APIGatewayProxyResponse> ConvertFiles(Stream inputStream, ILambdaContext context)
    {
        try
        {
            LambdaGlobals.Context = context;            
            string filename = "";
            Cloud.ConversionType convert = Cloud.ConversionType.None;

            LambdaGlobals.S3 = new S3Storage(LambdaGlobals.S3Client, context);

            TextReader textReader = new StreamReader(inputStream);
            var request = await textReader.ReadToEndAsync();
            LambdaGlobals.Context?.Logger.Log("Request is:" + textReader.ReadToEnd());
            JObject obj = JsonConvert.DeserializeObject<JObject>(request);
            if (obj.ContainsKey("queryStringParameters"))  // When triggered by Http request
            {
                LambdaGlobals.Context?.Logger.Log("Triggered by http request");

                APIGatewayProxyRequest apiRequest = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(request);

                var query = apiRequest.QueryStringParameters;
                if (query == null)
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = JsonConvert.SerializeObject(new { query, message = "File missing or not found" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
                    };
                if (!query.ContainsKey("filename") || !query.ContainsKey("bucket") || !query.ContainsKey("conversion"))
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = JsonConvert.SerializeObject(new { query, message = "Missing Parameters" }),
                        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
                    };
                LambdaGlobals.Bucket = query["bucket"];
                filename = query["filename"];
                convert |= StringToConversionType(query["conversion"]);
                await GetConversionJson(LambdaGlobals.Bucket, context);
            }
            else if (obj.ContainsKey("Records"))  //When triggered by S3 upload event
            {
                LambdaGlobals.Context?.Logger.Log("Triggered by S3 Event");
                dynamic s3Event = JsonConvert.DeserializeObject(request);
                LambdaGlobals.Bucket = s3Event.Records[0].s3.bucket.name;

                filename = s3Event.Records[0].s3.@object.key;
                await GetConversionJson(LambdaGlobals.Bucket, context);
                convert = Config.GetConversions();
                /*if (Config.ConfigJson.ContainsKey("InfluxDB") && Config.ConfigJson.InfluxDB.ContainsKey("enabled") && (Config.ConfigJson.InfluxDB.enabled == true))
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
                    convert |= Cloud.ConversionType.Blf;*/
                if (convert == 0)
                    LambdaGlobals.Context?.Logger.Log("No conversion settings enabled in Config.json");
            }
            else
            {
                LambdaGlobals.Context?.Logger.Log("RECORDS NOT found");
            }
            LambdaGlobals.FilePath = Path.GetDirectoryName(filename);
            LambdaGlobals.FileName = filename.Replace("+", " ");
            LambdaGlobals.FilePath ??= "";
            LambdaGlobals.LoggerDir = filename.Substring(0, filename.IndexOf('/'));
            LambdaGlobals.Context?.Logger.LogInformation("Processing file : " + LambdaGlobals.FileName);
            if(Config.ConfigJson.ContainsKey("outputBucket")){
                LambdaGlobals.OutputBucket = Config.ConfigJson.outputBucket;
            }
            await LambdaGlobals.S3Client.GetObjectMetadataAsync(LambdaGlobals.Bucket, LambdaGlobals.FileName);

            bool res = false;
            LambdaGlobals.Context?.Logger.LogInformation($"Bucket :{LambdaGlobals.Bucket}   Path: {filename}");
            //if (LambdaGlobals.FileName.Contains(".json") && convert.HasFlag(Cloud.ConversionType.Snapshot))
            //    res = (bool)await WriteSnapshot(LambdaGlobals.Bucket);
            if (LambdaGlobals.FileName.Contains(".rxd") || LambdaGlobals.FileName.Contains("Configuration.xml") || LambdaGlobals.FileName.Contains(".json"))
            {
                if (LambdaGlobals.FileName.Contains("Configuration.xml"))
                    convert = ConversionType.Rxc;
                var log = new AwsLogProvider(LambdaGlobals.Context);
                CloudConverter rxdConverter = new CloudConverter(log
                    , new AwsS3StorageProvider(LambdaGlobals.S3Client)
                    , new AwsTimeStreamProvider(log)
                    ,LambdaGlobals.Bucket
                    ,LambdaGlobals.LoggerDir
                    ,LambdaGlobals.OutputBucket
                    );
                res = (bool) await rxdConverter.Convert(LambdaGlobals.LoggerDir, LambdaGlobals.FileName, convert);
            }

            if (res)
                return CreateResponse(HttpStatusCode.OK, "Successfully converted input");
            else
                return CreateResponse(HttpStatusCode.InternalServerError, "Error converting input. Check log for details.");
        }
        catch (Exception exc)
        {
            LambdaGlobals.Context?.Logger.LogInformation($"Json Exception :{exc.Message}");
            throw;
        }
    }

    private APIGatewayProxyResponse CreateResponse(HttpStatusCode statusCode, string msg)
    {
        APIGatewayProxyResponse response = new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonConvert.SerializeObject(new { message = msg }),
            Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
        };
        return response;
    }

     

    

}
