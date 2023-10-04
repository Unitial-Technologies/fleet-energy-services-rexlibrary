using Amazon.Lambda.Core;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxShared.FileObjects;
using InfluxShared.Helpers;
using System.Globalization;

namespace AWSLambdaFileConvert.ExportFormats
{

    internal static class InfluxDBHelper
    {

        public static async Task<bool> ToInfluxDB(this DoubleDataCollection ddc)
        {
            static string PrepareChannelName(string chname)
            {
                string AllowedVariableNameChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                string tmp = chname.ReplaceInvalid(AllowedVariableNameChars.ToCharArray(), "_");

                tmp = tmp.Trim("_".ToCharArray());
                while (tmp.IndexOf("__") != -1)
                    tmp = tmp.Replace("__", "_");

                if (tmp.Length == 0 || ((tmp[0] >= '0') && (tmp[0] <= '9')))
                    tmp = "_" + tmp;

                return tmp;
            }

            try
            {
                var ci = new CultureInfo("en-US", false);

                using (var client = new InfluxDBClient(LambdaGlobals.InfluxDB.url, LambdaGlobals.InfluxDB.token))
                {
                    client.EnableGzip();
                    var writeApi = client.GetWriteApiAsync();
                    var idbData = new List<string>();
                    var idbFields = new List<string>();

                    ddc.InitReading();
                    //ddc.RealTime = DateTime.Now.AddHours(-3); //Used to overwrite time, so that older files can be written to DB

                    double[] Values = ddc.GetValues();
                    while (Values != null)
                    {
                        idbFields.Clear();
                        for (int i = 1; i < Values.Length; i++)
                            if (!double.IsNaN(Values[i]))
                                idbFields.Add(PrepareChannelName(ddc[i - 1].ChannelName) + "=" + Values[i].ToString(ci));

                        if (idbFields.Count > 0)
                        {
                            idbData.Add(
                                ddc.DisplayName + " " +
                                String.Join(",", idbFields.ToArray()) + " " +
                                Math.Truncate(((ddc.RealTime.ToOADate() - 25569) * 86400 + Values[0]) * 1000).ToString()
                                );

                            if (idbData.Count >= 5000)
                            {
                                await writeApi.WriteRecordsAsync(idbData, WritePrecision.Ms, LambdaGlobals.InfluxDB.bucket, LambdaGlobals.InfluxDB.org);
                                LambdaGlobals.Context?.Logger.LogInformation($"{idbData.Count} records written");
                                idbData.Clear();
                            }
                        }

                        Values = ddc.GetValues();
                    }
                    if (idbData.Count > 0)
                    {
                        await writeApi.WriteRecordsAsync(idbData, WritePrecision.Ms, LambdaGlobals.InfluxDB.bucket, LambdaGlobals.InfluxDB.org);
                        LambdaGlobals.Context?.Logger.LogInformation($"{idbData.Count} records written");
                        idbData.Clear();
                    }

                    //client.GetWriteApi().SetAnyProperty()
                };

                

                return true;
            }
            catch (Exception e)
            {
                LambdaGlobals.Context?.Logger.LogInformation(e.Message);
                return false;
            }

        }

      

    }
}
