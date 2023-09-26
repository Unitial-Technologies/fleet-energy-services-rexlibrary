using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using AWSLambdaFileConvert;
using AWSLambdaFileConvert.ExportFormats;
using DbcParserLib;
using DbcParserLib.Influx;
using InfluxDB.Client.Api.Domain;
using InfluxShared.FileObjects;
using InfluxShared.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RXD.Base;
using System.Net;
using System.Net.Sockets;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaConvert;

[Flags]
public enum ConversionType
{
    None = 0,
    Csv = 1,
    TimeStream = 2,
    InfluxDB = 4,
    Snapshot = 8,
    Rxc = 16
}

public class Function
{
    IAmazonS3 S3Client { get; set; }
    ILambdaContext? Context { get; set; }
    S3Storage S3; 
    string FilePath { get; set; } = "";
    string FileName { get; set; } = "";
    string LoggerDir { get; set; } = "";
    dynamic ConfigJson;
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();        
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
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

    ConversionType StringToConversionType(string conversion)
    {
        if (conversion.ToLower() == "snapshot")
            return ConversionType.Snapshot;
        else if (conversion.ToLower() == "csv")
            return ConversionType.Csv;
        else if (conversion.ToLower() == "amazontimestream")
            return ConversionType.TimeStream;
        else if (conversion.ToLower() == "influxdb")
            return ConversionType.InfluxDB;
        else if (conversion.ToLower() == "rxc")
            return ConversionType.Rxc;
        else
            return ConversionType.None;
    }

    async Task<bool> GetConversionJson(string bucket)
    {
        var jsonStream = await S3.GetStream(bucket, "FileConvert.json");
        using (StreamReader reader = new(jsonStream))
            ConfigJson = JsonConvert.DeserializeObject(reader.ReadToEnd());
        return true;
    }

    //By using Stream as a parameter the function can be triggered by both S3 Event and Http request
    public async Task<APIGatewayProxyResponse> ConvertFiles(Stream inputStream, ILambdaContext context)
    {
        try
        {
            this.Context = context;
            string bucket = "";
            string filename = "";
            ConversionType convert = ConversionType.None;

            S3 = new S3Storage(S3Client, Context);

            TextReader textReader = new StreamReader(inputStream);
            var request = await textReader.ReadToEndAsync();
            Context?.Logger.Log("Request is:" + textReader.ReadToEnd());
            JObject obj = JsonConvert.DeserializeObject<JObject>(request);
            if (obj.ContainsKey("queryStringParameters"))  // When triggered by Http request
            {
                Context?.Logger.Log("Triggered by http request");

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
                bucket = query["bucket"];
                filename = query["filename"];
                convert |= StringToConversionType(query["conversion"]);
                await GetConversionJson(bucket);
            }
            else if (obj.ContainsKey("Records"))  //When triggered by S3 upload event
            {
                Context?.Logger.Log("Triggered by S3 Event");
                dynamic s3Event = JsonConvert.DeserializeObject(request);
                bucket = s3Event.Records[0].s3.bucket.name;
                filename = s3Event.Records[0].s3.@object.key;
                await GetConversionJson(bucket);

                if (ConfigJson.ContainsKey("InfluxDB") && ConfigJson.InfluxDB.ContainsKey("enabled") && (ConfigJson.InfluxDB.enabled == true))
                    convert |= ConversionType.InfluxDB;
                if (ConfigJson.ContainsKey("CSV") && ConfigJson.CSV.ContainsKey("enabled") && (ConfigJson.CSV.enabled == true))
                    convert |= ConversionType.Csv;
                if (ConfigJson.ContainsKey("AmazonTimestream") && ConfigJson.AmazonTimestream.ContainsKey("enabled") && (ConfigJson.AmazonTimestream.enabled == true))
                    convert |= ConversionType.TimeStream;
                if (ConfigJson.ContainsKey("Snapshot") && ConfigJson.Snapshot.ContainsKey("enabled") && (ConfigJson.Snapshot.enabled == true))
                    convert |= ConversionType.Snapshot;
                else
                    Context?.Logger.Log("No conversion settings enabled in Config.json");
            }
            else
            {
                Context?.Logger.Log("RECORDS NOT found");
            }
            FilePath = Path.GetDirectoryName(filename);
            FileName = filename.Replace("+", " ");
            FilePath ??= "";
            LoggerDir = filename.Substring(0, filename.IndexOf('/'));
            Context?.Logger.LogInformation("Processing file : " + FileName);
            await this.S3Client.GetObjectMetadataAsync(bucket, FileName);

            bool res = false;
            Context?.Logger.LogInformation($"Bucket :{bucket}   Path: {filename}");
            if (FileName.Contains("Configuration.xml"))
                res = (bool)await ConvertXMLToRxc(bucket, FileName);
            else if (FileName.Contains(".json") && convert.HasFlag(ConversionType.Snapshot))
                res = (bool)await WriteSnapshot(bucket);
            else if (FileName.Contains(".rxd"))
                res = (bool)await ConvertRXD(bucket, FileName, convert);


            if (res)
                return CreateResponse(HttpStatusCode.OK, "Successfully converted input");
            else
                return CreateResponse(HttpStatusCode.InternalServerError, "Error converting input. Check log for details.");
        }
        catch (Exception exc)
        {
            Context?.Logger.LogInformation($"Json Exception :{exc.Message}");
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

    public async Task<Stream> GetXSD(string bucket)
    {
        //The xsd schema must be in the same folder as the xml file
        return await S3.GetStream(bucket, Path.Combine(LoggerDir, "ReXConfig.xsd"));
    }

    public async Task<Stream> LoadDBC(string bucket, string dbcFilename)
    {
        //The dbc must be in the same folder as the rxd file
        return await S3.GetStream(bucket, Path.Combine(LoggerDir, dbcFilename));
    }

    public async Task<bool?> ConvertXMLToRxc(string bucket, string filename)
    {
        try
        {
            //Get the XML file name and bucket
            BinRXD rxd = BinRXD.Create();
            GetObjectResponse objResponse = new GetObjectResponse();
            string BucketName = bucket;
            string Key = filename;
            var request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = Key
            };
            objResponse = await S3Client.GetObjectAsync(request);
            using (Stream xml = objResponse.ResponseStream)
            {
                //Load the XSD schema needed for the XML verification
                //XSD schema has to be in the same folder as the XML file
                Stream xsd = await GetXSD(bucket);
                if (xsd is null)
                {
                    Context?.Logger.LogInformation("XSD Schema Not Found!");
                    return null;
                }
                try
                {
                    rxd.ReadXMLStructure(xml, xsd);
                }
                catch (Exception e)
                {
                    Context?.Logger.LogInformation("XML Parsing Failed: " + e.Message);
                    return null;
                } 
                //the converted configuration file (rxc) is written in a memory stream, so it can be written back to the S3
                MemoryStream rxc = new MemoryStream();
                try
                {
                    rxd.ToRXD(rxc);
                }
                catch (Exception e)
                {
                    Context?.Logger.LogInformation("RXC Conversion Failed: " + e.Message);
                    return null;
                } 
                if (await S3.UploadFileAsync(BucketName, Path.Combine(FilePath, "Configuration.rxc"), rxc.ToArray()))  //The logger is looking for a file named Config.rxc to update its structure
                    Context?.Logger.LogInformation("RXC File Uploaded Successfully");              
            }

            return true;
        }
        catch (Exception e)
        {
            Context?.Logger.LogInformation("Exception ConvertToRxc Message: " + e.Message);
            Context?.Logger.LogInformation("Stack Trace: " + e.StackTrace);
            return false;
        }
    }

    public async Task<bool> ConvertRXD(string bucket, string filename, ConversionType conversion)
    {  
        if (!conversion.HasFlag(ConversionType.Csv) && !conversion.HasFlag(ConversionType.InfluxDB) &&
                    !conversion.HasFlag(ConversionType.TimeStream))
        {
            Context?.Logger.LogInformation("No valid Conversion requested!");
            return false;
        }

        List<DBC?> dbcList = await LoadDBCList(bucket);
        Stream rxdStream = await S3.GetStream(bucket, FileName);

        ExportDbcCollection signalsCollection = DbcToInfluxObj.LoadExportSignalsFromDBC(dbcList);
        
        //Stream xsdStream = await GetXSD(s3Event);
        try
        {
            using (BinRXD rxd = BinRXD.Load("http://" + FileName, rxdStream))
            {                
                if (rxd is null)
                {
                    Context?.Logger.LogInformation("Error loading RXD file");
                    return false;
                }
                else
                {
                    var export = new BinRXD.ExportSettings()
                    {
                        StorageCache = StorageCacheType.Memory,
                        SignalsDatabase = new() { dbcCollection = signalsCollection },

                    };
                    foreach (var collection in export.SignalsDatabase.dbcCollection)
                    {
                        Context?.Logger.LogInformation($"ExportSettingsBUS:{collection.BusChannel} signals:{collection.Signals.Count}");
                        foreach (var item in collection.Signals)
                        {
                            Context?.Logger.LogInformation($"ExportSettingsBUS:{collection.BusChannel} signal:{item.Name}");
                        }
                    }
                    DoubleDataCollection ddc = rxd.ToDoubleData(export);

                    //Write to InfluxDB
                    if (conversion.HasFlag(ConversionType.InfluxDB))
                    {
                        Context?.Logger.LogInformation("InfluxDB");
                        InfluxDBHelper.Context = Context;
                        InfluxDBHelper.idbSettings iddSettings = ConfigJson.InfluxDB.ToObject<InfluxDBHelper.idbSettings>();
                        string vars = "";
                        var vardict = Environment.GetEnvironmentVariables();
                        foreach (var item in vardict.Keys)
                        {
                            vars += $"Key:{item} ";
                        }
                        Context?.Logger.LogInformation(vars);
                        iddSettings.token = Environment.GetEnvironmentVariable("influxdb_token");
                        if (iddSettings.token is null)
                        {
                            Context?.Logger.LogInformation("InfluxDB access token missing. Add the Influx DB token as a Environment variable named influxdb_token");
                            iddSettings.token = "";
                        }
                        if (iddSettings.bucket == "default")
                            iddSettings.bucket = bucket;
                        await ddc.ToInfluxDB(iddSettings);
                    }

                    //Write to timestream table
                    if (conversion.HasFlag(ConversionType.TimeStream))
                    {
                        Context?.Logger.LogInformation("Amazon");
                        TimeStreamHelper.Context = Context;
                        TimeStreamHelper.atsSettings atsSettings = ConfigJson.AmazonTimestream.ToObject<TimeStreamHelper.atsSettings>();
                        if (atsSettings.db_name == "default")
                            atsSettings.db_name = bucket;
                        if (atsSettings.table_name == "default")
                            atsSettings.table_name = "rxddata";
                        int idx = filename.LastIndexOf('/');
                        long timeCorrection = await GetUTCCorrection(bucket);
                        Context?.Logger.LogInformation($"Table is is: {atsSettings.table_name}");
                        //Context?.Logger.LogInformation($"Correction in seconds is: {timeCorrection}");
                        await ddc.ToAwsTimeStream(atsSettings, filename.Substring(idx + 1, filename.Length - idx - 5), timeCorrection);
                    }

                    //CSV Export
                    if (conversion.HasFlag(ConversionType.Csv))
                    {
                        CsvMultipartHelper.Context = Context;
                        await CsvMultipartHelper.ToCSVMultipart(ddc, S3Client, bucket, Path.ChangeExtension(FileName, ".csv"));
                    }                   
                }
            }
        }
        catch (Exception e)
        {
            Context?.Logger.LogInformation("Error processing RXD file: " + e.Message);
            return false;
        }
        return true;
    }

    private async Task<List<DBC?>> LoadDBCList(string bucket)
    {
        Context?.Logger.LogInformation("Loading DBC");
        List<DBC?> listDbc = new();
        for (int i = 0; i< 4; i++)
        {
            Stream dbcStream = await LoadDBC(bucket, $"dbc_can{i}.dbc");
            if (dbcStream is null)
            {
                Context?.Logger.LogInformation("DBC File Not Found!");
                listDbc.Add(null);
                continue;
            }
            Parser dbcParser = new();
            Dbc dbc = dbcParser.ParseFromStream(dbcStream);
            Context?.Logger.LogInformation("DBC Messages count:" + dbc.Messages.ToList().Count.ToString());

            if (dbc is null)
            {
                Context?.Logger.LogInformation("Error parsing DBC file");
                listDbc.Add(null);
                continue;
            }
            DBC influxDBC = (DbcToInfluxObj.FromDBC(dbc) as DBC);
            listDbc.Add(influxDBC);
        }
        return listDbc;
    }

    private async Task<bool> WriteSnapshot(string bucket)
    {        
        TimeStreamHelper.Context = Context;
        int startIndex = FilePath.IndexOf("_SN") + 3;
        string sn = FilePath.Substring(startIndex, 7);
        Context?.Logger.LogInformation($"Snapshot Timestream for SN{sn}");
        if (!FileName.ToLower().Contains("snapshot"))
        {
            Context?.Logger.LogInformation($"File {FileName} ignored. Not a snapshot.");
            return false;
        }
        var jsonStream = await S3.GetStream(bucket, FileName);
        string json;
        using (StreamReader reader = new(jsonStream))
            json = reader.ReadToEnd();
        TimeStreamHelper.atsSettings atsSettings = ConfigJson.Snapshot.ToObject<TimeStreamHelper.atsSettings>();
        if (atsSettings.db_name == "default")
            atsSettings.db_name = bucket;
        if (atsSettings.table_name == "default")
            atsSettings.table_name = "snapshot";
        await TimeStreamHelper.WriteSnapshot(sn, atsSettings, json);
        return true;
    }

    private async Task<long> GetUTCCorrection(string bucket)
    {
        var jsonstream = await S3.GetStream(bucket, Path.Combine(FilePath, "Status.json"));
        if (jsonstream is null)
            return 0;
        DateTime fileDateTime = await GetFileCreationDateTime(bucket, Path.Combine(FilePath, "Status.json"));
        if (fileDateTime > DateTime.MinValue)
        {
            using (StreamReader reader = new(jsonstream))
            {
                string json = reader.ReadToEnd();
                dynamic status = JsonConvert.DeserializeObject(json);
                Context?.Logger.LogInformation($"UTC Time is: {((DateTimeOffset)fileDateTime).ToUnixTimeSeconds()} ::: {fileDateTime.ToString()}");
                Context?.Logger.LogInformation($"Logger Time is: {status.RTC_UNIX} ::: {DateUtility.FromUnixTimestamp((ulong)status.RTC_UNIX)}");
                if (status != null)
                {
                    //if ((ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() < (ulong)status.RTC_UNIX)
                        return (long)status.RTC_UNIX - (long)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                }
            }
        }
        
        return 0;
    }

    async Task<DateTime> GetFileCreationDateTime(string bucket, string filename)
    {
        try
        {
            GetObjectMetadataRequest metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = filename
            };
            GetObjectMetadataResponse metadataResponse = await S3Client.GetObjectMetadataAsync(metadataRequest);

            DateTime creationDate = metadataResponse.LastModified;
            return creationDate;
        }
        catch (Exception ex)
        {
            return DateTime.MinValue;
        }
    }

}
