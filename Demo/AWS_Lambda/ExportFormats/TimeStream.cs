using Amazon.Lambda.Core;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using InfluxShared.FileObjects;
using Newtonsoft.Json;

namespace AWSLambdaFileConvert.ExportFormats
{
    internal static class TimeStreamHelper
    {
        internal class atsSettings
        {
            public bool enabled { get; set; }
            public string db_name { get; set; }
            public string table_name { get; set; }
        }

        public static ILambdaContext? Context { get; set; } //Used to write information to log filesS

        public static async Task<bool> ToAwsTimeStream(this DoubleDataCollection ddc, atsSettings settings, string filename, long correction)
        {
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = settings.db_name,
                TableName = settings.table_name,
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
                            long timeStamp = (long)Math.Truncate((((ddc.RealTime.ToOADate() - 25569) * 86400 + Values[0]) - correction) * 1000);
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
                                Context?.Logger.LogInformation($"Engine Temperature is: {Values[i]}");
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
                Context?.Logger.LogInformation(e.Message);
                return false;
            }
        }

        private static async Task<bool> LocalWriteRecordsAsync(this AmazonTimestreamWriteClient writeClient, WriteRecordsRequest request)
        {
           // Context?.Logger.LogInformation("Writing records");

            try
            {
                Context?.Logger.LogInformation($"Request is:{request}");
                WriteRecordsResponse response = await writeClient.WriteRecordsAsync(request);
                Context?.Logger.LogInformation($"Write records status code: {response.HttpStatusCode.ToString()}");
            }
            catch (RejectedRecordsException e)
            {
                Context?.Logger.LogInformation("RejectedRecordsException:" + e.ToString());
                foreach (RejectedRecord rr in e.RejectedRecords)
                    Context?.Logger.LogInformation("RecordIndex " + rr.RecordIndex + " : " + rr.Reason);

                Context?.Logger.LogInformation("Other records were written successfully. ");
            }
            catch (Exception e)
            {
                Context?.Logger.LogInformation("Write records failure:" + e.ToString());
            }

            return true;
        }

        public static async Task<bool>WriteSnapshot(string device_id, atsSettings settings, string json)
        {
            var writeClient = new AmazonTimestreamWriteClient();
            var writeRecordsRequest = new WriteRecordsRequest
            {
                DatabaseName = settings.db_name,
                TableName = settings.table_name,
                Records = new()
            };

            try
            {
                var snapshot = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                if (snapshot is null)
                {
                    Context?.Logger.LogInformation("Couldn't parse Snapshot json");
                    return false;
                }                
                List<Dimension> dimensions = new List<Dimension>
                {
                    new Dimension { Name = "device_id", Value = device_id }
                };
                long timeStamp = snapshot["RTC_UNIX"] * 1000;
                Context?.Logger.LogInformation($"Snapshot timestamp is: {(ulong)snapshot["RTC_UNIX"]}");
                Context?.Logger.LogInformation($"Created Dimension, writing to {settings.table_name}");
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
                    Context?.Logger.LogInformation($"Writing {writeRecordsRequest.Records.Count} records");
                    await writeClient.LocalWriteRecordsAsync(writeRecordsRequest);
                    Context?.Logger.LogInformation($"Records {writeRecordsRequest.Records.Count} written");
                    writeRecordsRequest.Records.Clear();
                }

                return true;
            }
            catch (Exception e)
            {
                Context?.Logger.LogInformation(e.Message);
                return false;
            }
        }
    }


}
