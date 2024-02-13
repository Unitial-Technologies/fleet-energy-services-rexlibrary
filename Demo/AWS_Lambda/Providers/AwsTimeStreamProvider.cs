using Amazon.S3.Model;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using InfluxShared.FileObjects;
using InfluxShared.Generic;
using Newtonsoft.Json;
using Cloud;
using System.Reactive;
using System.Drawing;
using InfluxDB.Client.Api.Domain;

namespace AWSLambdaFileConvert.Providers
{
    public class AwsTimeStreamProvider : ITimeStreamProvider
    {
        ILogProvider Log;
        public AwsTimeStreamProvider(ILogProvider log)
        {
            Log = log;
        }

        async Task<bool> ToTimeStream(TimeStreamItem item)
        {
            long timeCorrection = await GetUTCCorrection(LambdaGlobals.Bucket);
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = Cloud.Config.Timestream.db_name,
                TableName = Cloud.Config.Timestream.table_name,
                Records = new()
            };

            try
            {
                foreach (var point in item.Points)
                {
                    writeRecordsRequest.Records.Add(new Record
                    {
                        Dimensions = new List<Dimension>
                            {
                                new Dimension { Name = "device_id", Value = item.DeviceId, DimensionValueType = DimensionValueType.VARCHAR},
                                new Dimension { Name = "filename", Value = item.Filename, DimensionValueType = DimensionValueType.VARCHAR},
                                new Dimension { Name = "bus", Value = item.Bus, DimensionValueType = DimensionValueType.VARCHAR},
                            },
                        MeasureName = item.Name,
                        MeasureValue = point.Value.ToString(),
                        MeasureValueType = MeasureValueType.DOUBLE,
                        Time = point.Timestamp.ToString(),
                        TimeUnit = TimeUnit.MILLISECONDS,
                        Version = 1
                    });
                    if (writeRecordsRequest.Records.Count >= 90)
                    {
                        await LocalWriteRecordsAsync(writeClient, writeRecordsRequest);
                        writeRecordsRequest.Records.Clear();
                    }
                }
                if (writeRecordsRequest.Records.Count > 0)
                {
                    await LocalWriteRecordsAsync(writeClient, writeRecordsRequest);
                    writeRecordsRequest.Records.Clear();
                }
                return true;
            }
            catch (Exception e)
            {
                Log?.Log(e.Message);
                Log?.Log($"Error Record Debug: {JsonConvert.SerializeObject(item)}");
                return false;
            } 
        }
        public async Task<bool> ToTimeStream(DoubleDataCollection ddc, string filename)
        {
            int idx = filename.LastIndexOf('/');
            Log?.Log("Downsampling started");
            Log?.Log($"Memory before downsampling: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            long timeCorrection = await GetUTCCorrection(LambdaGlobals.Bucket);

            try
            {
                ddc.InitReading();
                double time = 0;
                double value = 0;
                foreach (DoubleData data in ddc)
                {
                    TimeStreamItem item = new TimeStreamItem();
                    item.DeviceId = ddc.DisplayName;
                    item.Filename = filename;
                    item.Bus = data.BusChannel;
                    item.Name = data.ChannelName;
                    bool res = true;
                    while (res)
                    {
                        data.TimeStream.Read(ref time);
                        res = data.DataStream.Read(ref value);
                        long timeStamp = (long)Math.Truncate((((ddc.RealTime.ToOADate() - 25569) * 86400 + time) - timeCorrection) * 1000);
                        PointD point = new PointD() { Timestamp = timeStamp, Value = value };
                        item.Points.Add(point);
                    }
                    if (Cloud.Config.Timestream.downsampling)
                    {
                        List<PointD> points = Downsampling.DouglasPeucker(item.Points, Cloud.Config.Timestream.downsampling_tolerance);
                        //Log?.Log($"Points reduced from {item.Points.Count} to {points.Count}");
                        //Log?.Log($"Memory consumed so far: {GC.GetTotalMemory(false) / (1024 * 1024)} MB");
                        item.Points = points;
                    }                    
                    Log?.Log($"Writing {item.Name} to Timestream");
                    await ToTimeStream(item);
                }              
                return true;
            }
            catch (Exception e)
            {
                Log?.Log(e.Message);               
                return false;
            }
        }

        private async Task<bool> LocalWriteRecordsAsync(AmazonTimestreamWriteClient writeClient, WriteRecordsRequest request)
        {

            try
            {
                //Log?.Log($"Request is:{request}");
                WriteRecordsResponse response = await writeClient.WriteRecordsAsync(request);
                //Log?.Log($"Write records status code: {response.HttpStatusCode.ToString()}");
            }
            catch (RejectedRecordsException e)
            {
                Log?.Log("RejectedRecordsException:" + e.ToString());
                foreach (RejectedRecord rr in e.RejectedRecords)
                    Log?.Log("RecordIndex " + rr.RecordIndex + " : " + rr.Reason);

                Log?.Log("Other records were written successfully. ");
            }
            catch (Exception e)
            {
                Log?.Log("Write records failure:" + e.ToString());
            }

            return true;
        }

        public async Task<bool> WriteSnapshot(string device_id, string json, string filename)
        {
            long timeCorrection = await GetUTCCorrection(LambdaGlobals.Bucket);
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = Cloud.Config.Snapshot.db_name,
                TableName = Cloud.Config.Snapshot.table_name,
                Records = new()
            };

            try
            {
                var snapshot = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                if (snapshot is null)
                {
                    Log?.Log("Couldn't parse Snapshot json");
                    return false;
                }
                List<Dimension> dimensions = new List<Dimension>
                {
                    new Dimension { Name = "device_id", Value = device_id, DimensionValueType = DimensionValueType.VARCHAR }
                };
                DateTime fileDateTime = await GetFileCreationDateTime(LambdaGlobals.Bucket, filename);
                long timeStamp = ((DateTimeOffset)fileDateTime).ToUnixTimeSeconds();//snapshot["RTC_UNIX"];// * 1000;
                Log?.Log($"Snapshot timestamp: {(ulong)snapshot["RTC_UNIX"]}   Corrected Timestamp: {timeStamp}");
                Log?.Log($"Created Dimension, writing to {Cloud.Config.Snapshot.table_name}");
                foreach (var signal in snapshot)
                {
                    if (signal.Key != "RTC_UNIX")
                    {
                        Record record = new Record
                        {
                            Dimensions = dimensions,
                            MeasureName = signal.Key,
                            MeasureValue = signal.Value.ToString(),
                            MeasureValueType = MeasureValueType.DOUBLE,
                            Time = timeStamp.ToString(),
                            TimeUnit = TimeUnit.SECONDS,
                            Version = 1
                        };
                        writeRecordsRequest.Records.Add(record); ;
                        Log?.Log($"Record: {JsonConvert.SerializeObject(record)}"); 
                    }
                }

                if (writeRecordsRequest.Records.Count > 0)
                {
                    Log?.Log($"Writing {writeRecordsRequest.Records.Count} records");
                    await LocalWriteRecordsAsync(writeClient, writeRecordsRequest);
                    Log?.Log($"Records {writeRecordsRequest.Records.Count} written");
                    writeRecordsRequest.Records.Clear();
                }

                return true;
            }
            catch (Exception e)
            {
                Log?.Log(e.Message);
                return false;
            }
        }

        async Task<long> GetUTCCorrection(string bucket)
        {
            var jsonstream = await LambdaGlobals.S3.GetStream(bucket, Path.Combine(LambdaGlobals.LoggerDir, "Status.json"));
            if (jsonstream is null)
                return 0;
            DateTime fileDateTime = await GetFileCreationDateTime(bucket, Path.Combine(LambdaGlobals.LoggerDir, "Status.json"));
            if (fileDateTime > DateTime.MinValue)
            {
                using (StreamReader reader = new(jsonstream))
                {
                    string json = reader.ReadToEnd();
                    dynamic status = JsonConvert.DeserializeObject(json);
                    Log?.Log($"File Time is: {((DateTimeOffset)fileDateTime).ToUnixTimeSeconds()} ::: {fileDateTime.ToString()}");
                    Log?.Log($"Logger Time is: {status.RTC_UNIX} ::: {DateUtility.FromUnixTimestamp((ulong)status.RTC_UNIX)}");
                    if (status != null)
                    {
                        //if ((ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() < (ulong)status.RTC_UNIX)
                        Log?.Log($"Correction is: {(long)status.RTC_UNIX - (long)((DateTimeOffset)fileDateTime).ToUnixTimeSeconds()}");
                        return (long)status.RTC_UNIX - (long)((DateTimeOffset)fileDateTime).ToUnixTimeSeconds();
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
                GetObjectMetadataResponse metadataResponse = await LambdaGlobals.S3Client.GetObjectMetadataAsync(metadataRequest);

                DateTime creationDate = metadataResponse.LastModified;
                return creationDate;
            }
            catch (Exception ex)
            {
                return DateTime.MinValue;
            }
        }        
    }
}
