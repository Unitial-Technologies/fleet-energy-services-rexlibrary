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

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace XMLConverter;

public class Function
{
    IAmazonS3 S3Client { get; set; }
    ILambdaContext? Context { get; set; }
    string FilePath { get; set; } = "";
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
        if (FilePath is null)
            FilePath = "";
        var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);

        Context?.Logger.LogInformation("Processing file : " + s3Event.Object.Key);
        if (s3Event.Object.Key.Contains("Configuration.xml"))
            await ConvertXMLToRxc(s3Event);
        else if (s3Event.Object.Key.Contains(".rxd"))
            await ConvertRXD(s3Event);

        return response.Headers.ContentType;
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
                if (await UploadFileAsync(BucketName, "Configuration.rxc", rxc.ToArray()))  //The logger is looking for a file named Config.rxc to update its structure
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
    public async Task<bool> UploadFileAsync(string bucketName, string fileName, byte[] file)
    {
        try
        {
            
            var fileTransferUtility = new TransferUtility(S3Client);
            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = bucketName,
                StorageClass = S3StorageClass.Standard,
                PartSize = file.Length,
                Key = Path.Combine(FilePath, fileName)  
            };

            using (var ms = new MemoryStream(file))
            {
                fileTransferUtilityRequest.InputStream = ms;
                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            }

            return true;
        }
        catch (AmazonS3Exception e)
        {
            Context?.Logger.LogInformation("Uploading RXC Failed: " + e.Message);
            return false;
        }
    }

    
    public async Task<Stream> GetXSD(S3Event.S3Entity s3Event)
    {
        //The xsd schema must be in the same folder as the xml file
        return await GetStream(s3Event.Bucket.Name, Path.Combine(FilePath, "ReXConfig.xsd"));
    }

    public async Task<Stream> LoadDBC(S3Event.S3Entity s3Event)
    {
        //The dbc must be in the same folder as the rxd file
        return await GetStream(s3Event.Bucket.Name, Path.Combine(FilePath, "ExportDBC.dbc"));
    }

    public async Task<Stream> GetStream(string bucketName, string fileName)
    {
        try
        {
            GetObjectResponse objResponse = new GetObjectResponse();
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = fileName
            };
            Context?.Logger.LogInformation("Requesting stream: " + fileName);
            objResponse = await S3Client.GetObjectAsync(request);
            return objResponse.ResponseStream;
        }
        catch (Exception e)
        {
            Context?.Logger.LogInformation("Couldn't load stream: " + e.Message);
            return null;
        }
    }

    public async Task<bool> ConvertRXD(S3Event.S3Entity s3Event)
    {
        Stream dbcStream = await LoadDBC(s3Event);
        if (dbcStream is null)
        {
            Context?.Logger.LogInformation("DBC File Not Found!");
            return false;
        }
        Dbc dbc = Parser.ParseFromStream(dbcStream);
        if (dbc is null)
        {
            Context?.Logger.LogInformation("Error parsing DBC file");
            return false;
        }
        DBC influxDBC = (DbcToInfluxObj.FromDBC(dbc) as DBC);
        Stream rxdStream = await GetStream(s3Event.Bucket.Name, s3Event.Object.Key);
        MemoryStream outStream = new MemoryStream();
        ExportDbcCollection signalsCollection = DbcToInfluxObj.LoadExportSignalsFromDBC(influxDBC);
        Context?.Logger.LogInformation("DBC Message Count: " + influxDBC.Messages.Count.ToString());
        Context?.Logger.LogInformation("RXD Stream Size: " + rxdStream.Length.ToString());
        //Stream xsdStream = await GetXSD(s3Event);
        try
        {
            using (BinRXD rxd = BinRXD.Load("http://" + s3Event.Object.Key, rxdStream))
            {                
                if (rxd is null)
                {
                    Context?.Logger.LogInformation("Error loading RXD file");
                    return false;
                }
                else
                {
                    Context?.Logger.LogInformation("RXD Loaded RXD Count: " + rxd.Count.ToString());
                    await DataHelper.Convert(rxd, new BinRXD.ExportSettings()
                    {
                        StorageCache = StorageCacheType.Memory,
                        SignalsDatabase = new() { dbcCollection = signalsCollection },
                    }, outStream, @"csv");
                    Context?.Logger.LogInformation("CSV Out Stream Size: " + outStream.Length.ToString());
                    await UploadFileAsync(s3Event.Bucket.Name, "FirstCSV.csv", outStream.ToArray());
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