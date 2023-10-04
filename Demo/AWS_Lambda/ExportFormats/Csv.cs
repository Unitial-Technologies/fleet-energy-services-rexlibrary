using Amazon.S3;
using Amazon.S3.Model;
using InfluxShared.FileObjects;
using RXD.Base;
using System.Globalization;
using System.Text;

namespace AWSLambdaFileConvert.ExportFormats
{
    internal static class CsvMultipartHelper
    {
        internal static async Task<bool> ToCsv (string bucket, string destFile, BinRXD rxd, ExportDbcCollection signalsCollection)
        {
            LambdaGlobals.Context?.Logger.LogInformation($"Start CSV convert");
            ProcessingRulesCollection rules = null;
            try
            {
                if (LambdaGlobals.ConfigJson.CSV.enabled == true && LambdaGlobals.ConfigJson.CSV.resampling.enabled == true)
                {
                    rules = new ProcessingRulesCollection()
                    {
                        GeneralRules = new ProcessingRules(rules)
                        {
                            InitialTimestamp = SyncTimestampLogic.Zero,
                            SampleAfterEnd = true,
                            SampleBeforeBeginning = true,
                            SamplingMethod = SamplingValueSource.LastValue,
                            SamplingRate = LambdaGlobals.ConfigJson.CSV.resampling.rate
                        }
                    };
                }
            }
            catch (Exception exc)
            {
                LambdaGlobals.Context?.Logger.LogInformation($"Processing Rule Exception {exc.Message}");
            }

            var exportCsv = new BinRXD.ExportSettings()
            {
                StorageCache = StorageCacheType.Memory,
                SignalsDatabase = new() { dbcCollection = signalsCollection },
                ProcessingRules = rules,
            };
            var ddcCsv = rxd.ToDoubleData(exportCsv);
            LambdaGlobals.Context?.Logger.LogInformation($"ddc count {ddcCsv.Count}");
            return await ddcCsv.ToCSVMultipart(LambdaGlobals.S3Client, bucket, destFile);
            
        }

        internal static async Task<bool> ToCSVMultipart(this DoubleDataCollection ddc, IAmazonS3 s3Client, string bucket, string key)
        {

            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();
            InitiateMultipartUploadResponse initResponse =
                await s3Client.InitiateMultipartUploadAsync(new()
                {
                    BucketName = bucket,
                    Key = key
                });

            try
            {
                LambdaGlobals.Context?.Logger.LogInformation($"Initiated multipart upload {initResponse.UploadId}");

                var ci = new CultureInfo("en-US", false);

                ddc.InitReading();
                LambdaGlobals.Context?.Logger.LogInformation($"ddc count {ddc.Count}");
                int partId = 1;
                MemoryStream csvStream = new MemoryStream();
                using (StreamWriter stream = new StreamWriter(csvStream, new UTF8Encoding(false), 1024, true))
                {
                    async Task S3Upload()
                    {
                        csvStream.Seek(0, SeekOrigin.Begin);
                        csvStream.Position = 0;
                        UploadPartRequest uploadRequest = new UploadPartRequest
                        {
                            BucketName = bucket,
                            Key = key,
                            UploadId = initResponse.UploadId,
                            PartNumber = partId,
                            InputStream = csvStream,
                            IsLastPart = csvStream.Length < 5 * 1024 * 1024,
                            PartSize = csvStream.Length >= 5 * 1024 * 1024 ? csvStream.Length : 5 * 1024 * 1024
                        };

                        // Upload a part and add the response to our list.
                        LambdaGlobals.Context?.Logger.LogInformation($"Uploading part {partId} with size {csvStream.Length}");
                        uploadResponses.Add(await s3Client.UploadPartAsync(uploadRequest));
                        LambdaGlobals.Context?.Logger.LogInformation($"Uploaded part {partId}");
                        partId++;
                        csvStream.Seek(0, SeekOrigin.Begin);
                        csvStream.Position = 0;
                        csvStream.SetLength(0);
                    }

                    stream.Write(
                        "Creation Time : " + ddc.RealTime.ToString("dd/MM/yy HH:mm") + Environment.NewLine +
                        "Time," + string.Join(",", ddc.Select(n => n.ChannelName)) + Environment.NewLine +
                        new string(',', ddc.Count) + Environment.NewLine +
                        new string(',', ddc.Count) + Environment.NewLine +
                        "sec," + string.Join(",", ddc.Select(n => n.ChannelUnits)) + Environment.NewLine
                    );

                    double[] Values = ddc.GetValues();
                    while (Values != null)
                    {
                        stream.WriteLine(
                            DateTime.FromOADate(ddc.RealTime.ToOADate() + Values[0] / 86400).ToString("dd/MM/yyyy HH:mm:ss.fff") +","+
                            string.Join(",", Values.Select(x => x.ToString(ci)).ToArray(), 1, Values.Length - 1).Replace("NaN", ""));

                        if (csvStream.Length >= 5 * 1024 * 1024)
                            await S3Upload();

                        Values = ddc.GetValues();
                    }
                    LambdaGlobals.Context?.Logger.LogInformation($"Final upload initiation. Length is:{csvStream.Length}");
                    if (csvStream.Length > 0)
                        await S3Upload();

                    CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                    {
                        BucketName = bucket,
                        Key = key,
                        UploadId = initResponse.UploadId
                    };
                    completeRequest.AddPartETags(uploadResponses);

                    // Complete the upload.
                    CompleteMultipartUploadResponse completeUploadResponse =
                        await s3Client.CompleteMultipartUploadAsync(completeRequest);
                }

                return true;
            }
            catch (Exception e)
            {
                LambdaGlobals.Context?.Logger.LogInformation($"An AmazonS3Exception was thrown: {e.Message}");
                
                // Abort the upload.
                AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest
                {
                    BucketName = bucket,
                    Key = key,
                    UploadId = initResponse.UploadId
                };
                await s3Client.AbortMultipartUploadAsync(abortMPURequest);
                
                return false;
            }
        }
    }
}
