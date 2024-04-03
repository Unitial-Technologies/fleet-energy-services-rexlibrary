using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaFileConvert
{
    internal class S3Storage
    {
        IAmazonS3 S3Client;
        ILambdaContext Context;
        public S3Storage(IAmazonS3 s3Client, ILambdaContext context)
        {
            Context = context;
            S3Client = s3Client;
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
                    Key = fileName,
                    
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
                Context?.Logger.LogInformation($"Requesting stream: {fileName} from bucket: {bucketName}");
                objResponse = await S3Client.GetObjectAsync(request);
                return objResponse.ResponseStream;
            }
            catch (Exception e)
            {
                Context?.Logger.LogInformation("Couldn't load stream: " + e.Message);
                return null;
            }
        }

       /* public async Task<bool> UploadMultipart(string bucketName, string fileName)
        {
            string payload = new String('*', chunkSizeBytes);

            // Initiate the request.
            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = fileName
            };

            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
            InitiateMultipartUploadResponse initResponse = new InitiateMultipartUploadResponse();
                // Open a stream to build the input.
            for (int i = 0; i < totalChunks; i++)
            {
                // Write the next chunk to the input stream.
                Console.WriteLine($"Writing chunk {i} of {totalChunks}");
                using (var stream = ToStream(payload))
                {
                    // Write the next chunk to s3.
                    UploadPartRequest uploadRequest = new UploadPartRequest
                    {
                        BucketName = BucketName,
                        Key = Key,
                        UploadId = initResponse.UploadId,
                        PartNumber = i + 1,
                        PartSize = chunkSizeBytes,
                        InputStream = stream,
                    };

                    uploadResponses.Add(s3Client.UploadPart(uploadRequest));
                }
            }

            // Complete the request.
            CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = BucketName,
                Key = Key,
                UploadId = initResponse.UploadId
            };

            completeRequest.AddPartETags(uploadResponses);
            s3Client.CompleteMultipartUpload(completeRequest);
        }

        private Stream ToStream(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }*/

    }

}
