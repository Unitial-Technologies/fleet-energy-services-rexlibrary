using Amazon.Lambda.Core;
using Amazon.S3.Model;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using InfluxDB.Client.Api.Domain;
using InfluxShared.FileObjects;
using InfluxShared.Generic;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace AWSLambdaFileConvert.ExportFormats
{
    internal static class TimeStreamHelper
    {

        public static async Task<bool> ToAwsTimeStream(this DoubleDataCollection ddc, string filename)
        {
            int idx = filename.LastIndexOf('/');
            LambdaGlobals.Context?.Logger.LogInformation($"Table is: {LambdaGlobals.Timestream.table_name}");

            long timeCorrection = await GetUTCCorrection(LambdaGlobals.Bucket);
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = LambdaGlobals.Timestream.db_name,
                TableName = LambdaGlobals.Timestream.table_name,
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
                            //long timeStamp = (long)Math.Truncate(((DateTime.Now.AddHours(-6).ToOADate() - 25569) * 86400 + Values[0]) * 1000); 
                            long timeStamp = (long)Math.Truncate((((ddc.RealTime.ToOADate() - 25569) * 86400 + Values[0]) - timeCorrection) * 1000);
                            //Context?.Logger.LogInformation($"Logger Timestamp is {(ddc.RealTime.ToOADate() - 25569) * 86400 + Values[0]}");
                            //Context?.Logger.LogInformation($"Corrected timestamp is: {timeStamp}");
                            writeRecordsRequest.Records.Add(new Record
                            {
                                Dimensions = new List<Dimension>
                                {
                                    new Dimension { Name = "device_id", Value = ddc.DisplayName },
                                    new Dimension { Name = "filename", Value = filename},
                                    new Dimension { Name = "bus", Value = ddc[i - 1].BusChannel},
                                },
                                MeasureName = ddc[i - 1].ChannelName,
                                MeasureValue = Values[i].ToString(),
                                MeasureValueType = MeasureValueType.DOUBLE,
                                Time = timeStamp.ToString()
                            });
                            if (ddc[i - 1].ChannelName == "Engine_temperature")
                                LambdaGlobals.Context?.Logger.LogInformation($"Engine Temperature is: {Values[i]}");
                            if (writeRecordsRequest.Records.Count >= 90)
                            {
                                // Context?.Logger.LogInformation($"Writing {writeRecordsRequest.Records.Count} records");
                                await writeClient.LocalWriteRecordsAsync(writeRecordsRequest);                               
                                writeRecordsRequest.Records.Clear();
                            }
                        }
                    Values = ddc.GetValues();
                }
                if (writeRecordsRequest.Records.Count > 0)
                {
                   // Context?.Logger.LogInformation($"Writing {writeRecordsRequest.Records.Count} records");
                    await writeClient.LocalWriteRecordsAsync(writeRecordsRequest);
                  //  Context?.Logger.LogInformation($"Records {writeRecordsRequest.Records.Count} written");
                    writeRecordsRequest.Records.Clear();
                }

                return true;
            }
            catch (Exception e)
            {
                LambdaGlobals.Context?.Logger.LogInformation(e.Message);
                return false;
            }
        }

        private static async Task<bool> LocalWriteRecordsAsync(this AmazonTimestreamWriteClient writeClient, WriteRecordsRequest request)
        {
           // Context?.Logger.LogInformation("Writing records");

            try
            {
                LambdaGlobals.Context?.Logger.LogInformation($"Request is:{request}");
                WriteRecordsResponse response = await writeClient.WriteRecordsAsync(request);
                LambdaGlobals.Context?.Logger.LogInformation($"Write records status code: {response.HttpStatusCode.ToString()}");
            }
            catch (RejectedRecordsException e)
            {
                LambdaGlobals.Context?.Logger.LogInformation("RejectedRecordsException:" + e.ToString());
                foreach (RejectedRecord rr in e.RejectedRecords)
                    LambdaGlobals.Context?.Logger.LogInformation("RecordIndex " + rr.RecordIndex + " : " + rr.Reason);

                LambdaGlobals.Context?.Logger.LogInformation("Other records were written successfully. ");
            }
            catch (Exception e)
            {
                LambdaGlobals.Context?.Logger.LogInformation("Write records failure:" + e.ToString());
            }

            return true;
        }

        static async Task<bool>WriteSnapshot(string device_id, string json)
        {
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = LambdaGlobals.Snapshot.db_name,
                TableName = LambdaGlobals.Snapshot.table_name,
                Records = new()
            };

            try
            {
                var snapshot = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                if (snapshot is null)
                {
                    LambdaGlobals.Context?.Logger.LogInformation("Couldn't parse Snapshot json");
                    return false;
                }                
                List<Dimension> dimensions = new List<Dimension>
                {
                    new Dimension { Name = "device_id", Value = device_id }
                };
                long timeStamp = snapshot["RTC_UNIX"] * 1000;
                LambdaGlobals.Context?.Logger.LogInformation($"Snapshot timestamp is: {(ulong)snapshot["RTC_UNIX"]}");
                LambdaGlobals.Context?.Logger.LogInformation($"Created Dimension, writing to {LambdaGlobals.Timestream.table_name}");
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
                            Time = timeStamp.ToString()
                        };
                        writeRecordsRequest.Records.Add(record); ;
                        //Context?.Logger.LogInformation($"Record: {JsonConvert.SerializeObject(record)}"); 
                    }
                }
                
                if (writeRecordsRequest.Records.Count > 0)
                {
                    LambdaGlobals.Context?.Logger.LogInformation($"Writing {writeRecordsRequest.Records.Count} records");
                    await writeClient.LocalWriteRecordsAsync(writeRecordsRequest);
                    LambdaGlobals.Context?.Logger.LogInformation($"Records {writeRecordsRequest.Records.Count} written");
                    writeRecordsRequest.Records.Clear();
                }

                return true;
            }
            catch (Exception e)
            {
                LambdaGlobals.Context?.Logger.LogInformation(e.Message);
                return false;
            }
        }

        public static async Task<bool> WriteSnapshot(string bucket)
        {

            int startIndex = LambdaGlobals.FilePath.IndexOf("_SN") + 3;
            string sn = LambdaGlobals.FilePath.Substring(startIndex, 7);
            LambdaGlobals.Context?.Logger.LogInformation($"Snapshot Timestream for SN{sn}");
            if (!LambdaGlobals.FileName.ToLower().Contains("snapshot"))
            {
                LambdaGlobals.Context?.Logger.LogInformation($"File {LambdaGlobals.FileName} ignored. Not a snapshot.");
                return false;
            }
            var jsonStream = await LambdaGlobals.S3.GetStream(bucket, LambdaGlobals.FileName);
            string json;
            using (StreamReader reader = new(jsonStream))
                json = reader.ReadToEnd();
            await WriteSnapshot(sn, json);
            return true;
        }

        static async Task<long> GetUTCCorrection(string bucket)
        {
            var jsonstream = await LambdaGlobals.S3.GetStream(bucket, Path.Combine(LambdaGlobals.FilePath, "Status.json"));
            if (jsonstream is null)
                return 0;
            DateTime fileDateTime = await GetFileCreationDateTime(bucket, Path.Combine(LambdaGlobals.FilePath, "Status.json"));
            if (fileDateTime > DateTime.MinValue)
            {
                using (StreamReader reader = new(jsonstream))
                {
                    string json = reader.ReadToEnd();
                    dynamic status = JsonConvert.DeserializeObject(json);
                    LambdaGlobals.Context?.Logger.LogInformation($"UTC Time is: {((DateTimeOffset)fileDateTime).ToUnixTimeSeconds()} ::: {fileDateTime.ToString()}");
                    LambdaGlobals.Context?.Logger.LogInformation($"Logger Time is: {status.RTC_UNIX} ::: {DateUtility.FromUnixTimestamp((ulong)status.RTC_UNIX)}");
                    if (status != null)
                    {
                        //if ((ulong)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() < (ulong)status.RTC_UNIX)
                        return (long)status.RTC_UNIX - (long)((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
                    }
                }
            }

            return 0;
        }

        static async Task<DateTime> GetFileCreationDateTime(string bucket, string filename)
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
