using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Minio.DataModel.Args;
using Minio;
using Newtonsoft.Json;
using Cloud;
using MinIOFileConvert.Providers;

namespace MinIOFileConvert
{
    internal class MinIO
    {
        internal string Bucket { get; set; } = "influx";
        internal string AccessKey { get; set; }
        internal string SecretKey { get; set; } 
        internal string EndPoint { get; set; }
        internal string ObjectName { get; set; }
        internal string LoggerDir { get; set; }
        internal string FilePath { get; set; }

        CloudConverter rxdConverter { get; }
        public MinIO(string key, string secret, string endPoint)
        {
            AccessKey = key;
            SecretKey = secret;
            EndPoint = endPoint;
            var minio = new MinioClient().WithCredentials(AccessKey, SecretKey).WithEndpoint(EndPoint).Build();
            rxdConverter = new CloudConverter(new MinIOLogProvider(), new MinIOStorageProvider(minio), null, "influx", "");            
        }

        internal async Task<bool> StartListening()
        {
            TcpListener server = null;
            try
            {
                Int32 port = 13000;
                IPAddress localAddr = IPAddress.Parse("0.0.0.0");

                server = new TcpListener(localAddr, port);
                server.Start();
                Byte[] bytes = new Byte[1054];
                String data;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    using TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = "";

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();
                    MemoryStream outputStream = new MemoryStream();
                    int readByteCount;
                    byte[] buffer = new byte[1024];

                    while ((readByteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputStream.Write(buffer, 0, readByteCount);
                        data += Encoding.ASCII.GetString(buffer, 0, readByteCount);
                        if (readByteCount < buffer.Length)
                            break;
                    }
                    Console.WriteLine("{0}", data);
                    if (data.Contains("s3:ObjectCreated:Put"))
                    {
                        data = data.Substring(data.IndexOf("EventName") - 2);
                        dynamic s3Event = JsonConvert.DeserializeObject(data);
                        if (s3Event.ContainsKey("EventName") && s3Event.ContainsKey("Key"))
                            if (s3Event.EventName.Value == "s3:ObjectCreated:Put")
                            {
                                ObjectName = s3Event.Key.Value;
                                string extension = Path.GetExtension(ObjectName).ToLower();
                                if (extension == ".xml" || extension == ".rxd" || extension == ".json")
                                {                                    
                                    string[] directories = ObjectName.Split('/');
                                    if (directories.Length < 3)
                                        return true;
                                    Bucket = directories[0];
                                    LoggerDir = directories[1];
                                    FilePath = Path.GetDirectoryName(ObjectName);
                                    if (extension == ".json")
                                    {
                                        var file = await rxdConverter.GetFile(Bucket, LoggerDir + "/Snapshot1.json");
                                        string json1 = "";
                                        if (file != null)
                                            using (StreamReader sr = new StreamReader(file))
                                                json1 = sr.ReadToEnd();
                                        JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json1);
                                        Console.WriteLine(json1);
                                    }
                                    

                                    var cfgJson = await rxdConverter.GetFile(Bucket, "FileConvert.json");
                                    Config.LoadSettings(cfgJson);
                                    
                                    if (Path.GetExtension(ObjectName).ToLower() == ".xml")
                                    {
                                        var res = await rxdConverter.Convert(LoggerDir, ObjectName.Replace(Bucket + '/', ""), ConversionType.Rxc);
                                        Console.WriteLine($"{ObjectName} {res.ToString()}");
                                    }
                                    else if (Path.GetExtension(ObjectName).ToLower() == ".rxd")
                                    {
                                        ConversionType conversion = Config.GetConversions();
                                        var res = await rxdConverter.Convert(LoggerDir, ObjectName.Replace(Bucket + '/', ""), conversion);
                                        Console.WriteLine($"{ObjectName} {res.ToString()}");
                                    }
                                }
                                
                            }
                    }
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                return false;
            }
            finally
            {
                server.Stop();
            }


            return true;
        }

        
    }
}
