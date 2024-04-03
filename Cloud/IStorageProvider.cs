using System;
using System.Collections.Generic;
using System.Text;

namespace Cloud
{
    public interface IStorageProvider
    {
        Task<Stream> GetFile(string bucket, string file);
        Task<bool> UploadFile(string bucket, string file, Stream stream);
        Task<string> InitiateMultipartUpload(string bucket, string file);
        Task<bool> UploadPart(MemoryStream fileStream, string bucket, string key, string uploadId, int partId);
        Task<bool> CompleteMultipartUpload(string bucket, string key, string uploadId);
        Task<bool> AbortMultipartUpload(string bucket, string key, string uploadId);
        Task<List<string>> GetRxdFiles(string bucket, string path, bool includeSubfolders = true);
    }
}
