using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Cloud;
using InfluxDB.Client.Api.Domain;
using Minio.DataModel;
using System.Net.Security;
using System.Net.Sockets;

namespace AWSLambdaFileConvert.Providers
{
    public class AwsS3StorageProvider : IStorageProvider
    {
        readonly IAmazonS3 _S3Client;
        string _LastError = "";
        List<UploadPartResponse> UploadResponses;
        public AwsS3StorageProvider(IAmazonS3 S3Client)
        {
            _S3Client = S3Client;
        }
        public string GetLastError()
        {
            return _LastError;
        }

        public async Task<bool> UploadFile(string bucket, string fileName, Stream stream)
        {
            try
            {
                byte[] fileArr;
                if (stream is MemoryStream)
                    fileArr = (stream as MemoryStream).ToArray();
                else
                {
                    MemoryStream ms = new();
                    stream.CopyTo(ms);
                    fileArr = ms.ToArray();
                }
                var fileTransferUtility = new TransferUtility(_S3Client);
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucket,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = stream.Length,
                    Key = fileName,

                };

                using (var ms = new MemoryStream(fileArr))
                {
                    fileTransferUtilityRequest.InputStream = ms;
                    await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                }

                return true;
            }
            catch (AmazonS3Exception e)
            {
                _LastError = e.Message;
                return false;
            }
        }


        public async Task<Stream> GetFile(string bucketName, string fileName)
        {
            try
            {
                GetObjectResponse objResponse = new GetObjectResponse();
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };
                objResponse = await _S3Client.GetObjectAsync(request);
                return objResponse.ResponseStream;
            }
            catch (Exception e)
            {
                _LastError = "Couldn't load stream: " + e.Message;
                return null;
            }
        }

        public async Task<string> InitiateMultipartUpload(string bucket, string file)
        {
            UploadResponses = new List<UploadPartResponse>();
            InitiateMultipartUploadResponse initResponse =
                await _S3Client.InitiateMultipartUploadAsync(new()
                {
                    BucketName = bucket,
                    Key = file
                });
            return initResponse.UploadId;
        }

        public async Task<bool> UploadPart(MemoryStream fileStream, string bucket, string key, string uploadId, int partId)
        {
            UploadPartRequest uploadRequest = new UploadPartRequest
            {
                BucketName = bucket,
                Key = key,
                UploadId = uploadId,
                PartNumber = partId,
                InputStream = fileStream,
                IsLastPart = fileStream.Length < 5 * 1024 * 1024,
                PartSize = fileStream.Length >= 5 * 1024 * 1024 ? fileStream.Length : 5 * 1024 * 1024
            };
            // Upload a part and add the response to our list.
            UploadResponses.Add(await _S3Client.UploadPartAsync(uploadRequest));
            return true;
        }

        public async Task<bool> CompleteMultipartUpload(string bucket, string key, string uploadId)
        {
            CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
            {
                BucketName = bucket,
                Key = key,
                UploadId = uploadId
            };
            completeRequest.AddPartETags(UploadResponses);

            // Complete the upload.
            CompleteMultipartUploadResponse completeUploadResponse =
                await _S3Client.CompleteMultipartUploadAsync(completeRequest);
            return true;
        }

        public async Task<bool> AbortMultipartUpload(string bucket, string key, string uploadId)
        {
            AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest
            {
                BucketName = bucket,
                Key = key,
                UploadId = uploadId
            };
            await _S3Client.AbortMultipartUploadAsync(abortMPURequest);
            return true;
        }

        public async Task<List<string>> GetRxdFiles(string bucket, string path, bool includeSubfolders = true)
        {            
            List<string> files = (List<string>) await _S3Client.GetAllObjectKeysAsync(bucket, path, null);            
            return files.Where(x=>x.EndsWith(".rxd")).ToList();
        }
    }
}
