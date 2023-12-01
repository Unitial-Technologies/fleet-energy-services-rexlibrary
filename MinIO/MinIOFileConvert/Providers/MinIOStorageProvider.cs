using Cloud;
using Minio;
using Minio.DataModel.Args;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinIOFileConvert.Providers
{
    internal class MinIOStorageProvider : IStorageProvider
    {
        readonly IMinioClient _Minio;
        public MinIOStorageProvider(IMinioClient minio)
        {
            _Minio = minio;
        }

        public Task<bool> AbortMultipartUpload(string bucket, string key, string uploadId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CompleteMultipartUpload(string bucket, string key, string uploadId)
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> GetFile(string bucketName, string objectName)
        {

            try
            {
                //var minio = new MinioClient().WithCredentials(AccessKey, SecretKey).WithEndpoint(EndPoint).Build();
                // {
                Stream fileStream = new MemoryStream();

                StatObjectArgs statObjectArgs = new StatObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName);
                await _Minio.StatObjectAsync(statObjectArgs);
                // Get the file as a stream
                var res = await _Minio.GetObjectAsync(new GetObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream((stream) =>
                    {
                        stream.CopyTo(fileStream);
                    }));

                fileStream.Position = 0;
                /* using (StreamReader reader = new StreamReader(fileStream))
                 {
                     string content = await reader.ReadToEndAsync();
                     Console.WriteLine("File content:");
                     Console.WriteLine(content);
                 }*/
                return fileStream;
                // }                
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e}");
                return null;
            }
        }

        public Task<string> InitiateMultipartUpload(string bucket, string file)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UploadFile(string bucketName, string objectName, Stream stream)
        {
            await _Minio.PutObjectAsync(
                new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length));
            return true;
        }

        public Task<bool> UploadPart(MemoryStream fileStream, string bucket, string key, string uploadId, int partId)
        {
            throw new NotImplementedException();
        }
    }
}
