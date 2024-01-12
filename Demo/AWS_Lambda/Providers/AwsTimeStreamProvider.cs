using Amazon.S3.Model;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using InfluxShared.FileObjects;
using InfluxShared.Generic;
using Newtonsoft.Json;
using Cloud;

namespace AWSLambdaFileConvert.Providers
{
    public class AwsTimeStreamProvider : ITimeStreamProvider
    {
        ILogProvider Log;
        public AwsTimeStreamProvider(ILogProvider log)
        {
            Log = log;
        }
        public async Task<bool> ToTimeStream(DoubleDataCollection ddc, string filename)
        {
            int idx = filename.LastIndexOf('/');
            Log?.Log($"Table is: {Config.Timestream.table_name}");

            long timeCorrection = await GetUTCCorrection(LambdaGlobals.Bucket);
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = Config.Timestream.db_name,
                TableName = Config.Timestream.table_name,
                Records = new()
            };

            try
            {
                ddc.InitReading();


                double[] Values = ddc.GetValues();
                while (Values != null)
                {
                    for (int i = 1; i < Values.Length; i++)
                        if (!double.IsNaN(Values[i]))
                        {
                            //long timeStamp = (long)Math.Truncate(((DateTime.Now.AddHours(-6).ToOADate() - 25569) * 86400 + Values[0]) * 1000);   //
                            long timeStamp = (long)Math.Truncate((((ddc.RealTime.ToOADate() - 25569) * 86400 + Values[0]) - timeCorrection) * 1000);
                            //Context?.Logger.LogInformation($"Logger Timestamp is {(ddc.RealTime.ToOADate() - 25569) * 86400 + Values[0]}");
                            //Context?.Logger.LogInformation($"Corrected timestamp is: {timeStamp}");
                            writeRecordsRequest.Records.Add(new Record
                            {
                                Dimensions = new List<Dimension>
                                {
                                    new Dimension { Name = "device_id", Value = ddc.DisplayName, DimensionValueType = DimensionValueType.VARCHAR},
                                    new Dimension { Name = "filename", Value = filename, DimensionValueType = DimensionValueType.VARCHAR},
                                    new Dimension { Name = "bus", Value = ddc[i - 1].BusChannel, DimensionValueType = DimensionValueType.VARCHAR},
                                },
                                MeasureName = ddc[i - 1].ChannelName,
                                MeasureValue = Values[i].ToString(),
                                MeasureValueType = MeasureValueType.DOUBLE,
                                Time = timeStamp.ToString(),
                                TimeUnit = TimeUnit.MILLISECONDS,
                                Version = 1
                            });
                            //Log?.Log($"Bus is: {ddc[i-1].BusChannel}");
                            //Log?.Log($"Dimension: {string.Join(";", writeRecordsRequest.Records.Last().Dimensions.Select(d => $"{d.Name}={d.Value}"))}");
                            //if (ddc[i - 1].ChannelName == "Engine_temperature")
                            //    Log?.Log($"Engine Temperature is: {Values[i]}");
                            if (writeRecordsRequest.Records.Count >= 90)
                            {
                                //Log?.Log($"Writing {writeRecordsRequest.Records.Count} records");
                                //Log?.Log($"Writing {writeRecordsRequest.} records");
                                await LocalWriteRecordsAsync(writeClient, writeRecordsRequest);
                                foreach (var item in writeRecordsRequest.Records)
                                {
                                    Log?.Log($"Record: {JsonConvert.SerializeObject(item)}");
                                }
                                writeRecordsRequest.Records.Clear();
                            }
                        }
                    Values = ddc.GetValues();
                }
                if (writeRecordsRequest.Records.Count > 0)
                {
                    // Context?.Logger.LogInformation($"Writing {writeRecordsRequest.Records.Count} records");
                    await LocalWriteRecordsAsync(writeClient, writeRecordsRequest);
                    //  Context?.Logger.LogInformation($"Records {writeRecordsRequest.Records.Count} written");
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

        private async Task<bool> LocalWriteRecordsAsync(AmazonTimestreamWriteClient writeClient, WriteRecordsRequest request)
        {
            // Context?.Logger.LogInformation("Writing records");

            try
            {
                Log?.Log($"Request is:{request}");
                WriteRecordsResponse response = await writeClient.WriteRecordsAsync(request);
                Log?.Log($"Write records status code: {response.HttpStatusCode.ToString()}");
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
                DatabaseName = Config.Snapshot.db_name,
                TableName = Config.Snapshot.table_name,
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
                Log?.Log($"Created Dimension, writing to {Config.Snapshot.table_name}");
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
                    Log?.Log($"UTC Time is: {((DateTimeOffset)fileDateTime).ToUnixTimeSeconds()} ::: {fileDateTime.ToString()}");
                    Log?.Log($"Logger Time is: {status.RTC_UNIX} ::: {DateUtility.FromUnixTimestamp((ulong)status.RTC_UNIX)}");
                    if (status != null)
                    {
                        //if ((ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() < (ulong)status.RTC_UNIX)
                        return (long)status.RTC_UNIX - (long)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
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
