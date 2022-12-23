using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using RXD.Base;
using Influx.Shared.Helpers;
using InfluxShared.FileObjects;
using DbcParserLib;
using DbcParserLib.Influx;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using Amazon;
using AWSLambdaFileConvert;
using AWSLambdaFileConvert.ExportFormats;
using Newtonsoft.Json;
using System.Text;
using Cloud.Grafana;
using System.IO;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaConvert;

public class Function
{
    IAmazonS3 S3Client { get; set; }
    ILambdaContext? Context { get; set; }
    S3Storage S3; 
    string FilePath { get; set; } = "";
    string FileName { get; set; } = "";
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
    public async Task<string?> ConvertFiles(S3Event evnt, ILambdaContext context)
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

        var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, FileName);

        Context?.Logger.LogInformation("Processing file : " + FileName);

        if (FileName.Contains("Configuration.xml"))
            await ConvertXMLToRxc(s3Event);
        else if (FileName.Contains(".rxd"))
            await ConvertRXD(s3Event);

        return response.Headers.ContentType;
    }

    public async Task<Stream> GetXSD(S3Event.S3Entity s3Event)
    {
        //The xsd schema must be in the same folder as the xml file
        return await S3.GetStream(s3Event.Bucket.Name, Path.Combine(FilePath, "ReXConfig.xsd"));
    }

    public async Task<Stream> LoadDBC(S3Event.S3Entity s3Event)
    {
        //The dbc must be in the same folder as the rxd file
        return await S3.GetStream(s3Event.Bucket.Name, Path.Combine(FilePath, "ExportDBC.dbc"));
    }

    public async Task<bool?> ConvertXMLToRxc(S3EventNotification.S3Entity s3Event)
    {
        try
        {
            //Get the XML file name and bucket
            BinRXD rxd = BinRXD.Create();
            GetObjectResponse objResponse = new GetObjectResponse();
            string BucketName = s3Event.Bucket.Name;
            string Key = s3Event.Object.Key;
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
                Stream xsd = await GetXSD(s3Event);
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
    

    
    

    public async Task<bool> ConvertRXD(S3Event.S3Entity s3Event)
    {
        var jsonStream = await S3.GetStream(s3Event.Bucket.Name, "FileConvert.json");
        dynamic json;
        using (StreamReader reader = new(jsonStream))
            json = JsonConvert.DeserializeObject(reader.ReadToEnd());

        bool UseInfluxDB = json.ContainsKey("InfluxDB") && json.InfluxDB.ContainsKey("enabled") && (json.InfluxDB.enabled == true);
        bool UseCSV = json.ContainsKey("CSV") && json.CSV.ContainsKey("enabled") && (json.CSV.enabled == true);
        bool UseAmazonTimestream = json.ContainsKey("AmazonTimestream") && json.AmazonTimestream.ContainsKey("enabled") && (json.AmazonTimestream.enabled == true);

        if (!UseInfluxDB && !UseCSV && !UseAmazonTimestream)
            return false;

        Context?.Logger.LogInformation("Loading DBC");
        Stream dbcStream = await LoadDBC(s3Event);
        if (dbcStream is null)
        {
            Context?.Logger.LogInformation("DBC File Not Found!");
            return false;
        }
        Dbc dbc = Parser.ParseFromStream(dbcStream);
        Context?.Logger.LogInformation("DBC Messages count:" + dbc.Messages.ToList().Count.ToString());

        if (dbc is null)
        {
            Context?.Logger.LogInformation("Error parsing DBC file");
            return false;
        }
        DBC influxDBC = (DbcToInfluxObj.FromDBC(dbc) as DBC);
        Stream rxdStream = await S3.GetStream(s3Event.Bucket.Name, FileName);

        ExportDbcCollection signalsCollection = DbcToInfluxObj.LoadExportSignalsFromDBC(influxDBC);
        Context?.Logger.LogInformation("DBC Message Count: " + influxDBC.Messages.Count.ToString());        
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
                    DoubleDataCollection ddc = rxd.ToDoubleData(new BinRXD.ExportSettings()
                    {
                        StorageCache = StorageCacheType.Memory,
                        SignalsDatabase = new() { dbcCollection = signalsCollection },

                    });

                    //Write to InfluxDB
                    if (UseInfluxDB)
                    {
                        Context?.Logger.LogInformation("InfluxDB");
                        InfluxDBHelper.Context = Context;
                        InfluxDBHelper.idbSettings iddSettings = json.InfluxDB.ToObject<InfluxDBHelper.idbSettings>();
                        await ddc.ToInfluxDB(iddSettings);
                    }

                    //Write to timestream table
                    if (UseAmazonTimestream)
                    {
                        Context?.Logger.LogInformation("Amazon");
                        TimeStreamHelper.Context = Context;
                        TimeStreamHelper.atsSettings atsSettings = json.AmazonTimestream.ToObject<TimeStreamHelper.atsSettings>();
                        await ddc.ToAwsTimeStream(atsSettings);
                    }

                    //CSV Export
                    if (UseCSV)
                    {
                        CsvMultipartHelper.Context = Context;
                        await CsvMultipartHelper.ToCSVMultipart(ddc, S3Client, s3Event.Bucket.Name, Path.ChangeExtension(FileName, ".csv"));
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

    
}
