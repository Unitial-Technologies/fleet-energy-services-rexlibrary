using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using Newtonsoft.Json;
using System.Text;
using MinIOFileConvert;
using Cloud;

class Program
{
    static async Task Main(string[] args)
    {
        MinIO minio = new MinIO("F1Dq0gb4TSkCougRjizi", "QMUYCozGndTICECmAPE7VdqdC26Z86wYdAmNKVUo", "localhost:9000");
        await minio.StartListening();
        string bucketName = "influx";
        string objectName = "FileConvert.json"; // e.g., "folder/file.txt"
        


        // await UploadFile(bucketName, objectName);
    }

    private static async Task GetFile(string bucketName, string objectName)
    {
        // Set your MinIO server credentials
        string endPoint = "localhost:9000"; // e.g., "play.min.io"
        string accessKey = "TXlZPLy2rjY1pEc5buiM";
        string secretKey = "epe0IrvIoLd7Zc655HPlzkh1v9mdpusyok4MQSBK";

        // Initialize the MinIO client object
        var minio = new MinioClient().WithCredentials(accessKey, secretKey).WithEndpoint(endPoint).Build();
        var list = await minio.ListBucketsAsync();
        foreach (var item in list.Buckets)
        {
            Console.WriteLine(item.Name);
        }

        try
        {
            Stream fileStream = new MemoryStream();

            StatObjectArgs statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            await minio.StatObjectAsync(statObjectArgs);

            // Get the file as a stream
            var res = await minio.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream((stream) =>
            {
                stream.CopyTo(fileStream);
            }));

            fileStream.Position = 0;
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string content = await reader.ReadToEndAsync();
                Console.WriteLine("File content:");
                Console.WriteLine(content);
            }

            fileStream.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}");
        }
    }

  

    static async Task UploadFile(string bucketName, string objectName)
    {
        // Set your MinIO server credentials
        string endPoint = "localhost:9000"; // e.g., "play.min.io"
        string accessKey = "TXlZPLy2rjY1pEc5buiM";
        string secretKey = "epe0IrvIoLd7Zc655HPlzkh1v9mdpusyok4MQSBK";
        var minio = new MinioClient().WithCredentials(accessKey, secretKey).WithEndpoint(endPoint).Build();
        MemoryStream ms = new MemoryStream(File.ReadAllBytes(@"C:\Users\PPetkov\Downloads\FileConvert.json"));

        await minio.PutObjectAsync(
            new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length));
    }
}