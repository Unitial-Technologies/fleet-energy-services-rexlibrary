using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaFileConvert.ExportFormats
{
    internal class Json
    {
        List<JsonItem> items = new List<JsonItem>();

        
        public async Task<dynamic> ConvertToJson(DoubleDataCollection ddc)
        {
            /* ddc.InitReading();
             JsonItem item = new JsonItem();
             dynamic channelName = new ExpandoObject();
             item.channel.name = channelName;
             items.Add(item);
             var dictNames = (IDictionary<string, object>)channelName;
             for (int i = 0; i< ddc.Count; i++)
             {
                 DoubleData dd = ddc[i];
                 dictNames.Add("channel" + i.ToString(), dd.ChannelName);
             }

             var dictUnits = (IDictionary<string, object>)channelName;
             for (int i = 0; i < ddc.Count; i++)
             {
                 DoubleData dd = ddc[i];
                 dictUnits.Add("units" + i.ToString(), dd.ChannelUnits);
             }
             item.channel.unit = dictUnits;

             double[] Values = ddc.GetValues();
             var feed = new Feed();
             while (Values != null)
             {
                 dynamic data = new ExpandoObject();
                 feed.time = DateTime.FromOADate(ddc.RealTime.ToOADate() + Values[0] / 86400);
                 var itemvalues = (IDictionary<string, object>)data;

                 for (int i = 1; i < Values.Length; i++)
                     if (!double.IsNaN(Values[i]))
                     {                       
                         itemvalues.Add(ddc[i].ChannelName, Values[i].ToString());
                     }

                 feed.value = data;

                 Values = ddc.GetValues();
             }
             item.feeds.Add(feed);

             return items;*/

            JsonItem channels = new JsonItem();
            ddc.InitReading();
            channels.channel.description = "Json from me";
            //var dictionary = (IDictionary<string, object>)channelInfo;
            channels.channel.name = "first json";
           /* for (int i = 0; i < ddc.Count; i++)
            {
                DoubleData dd = ddc[i];
                dictionary.Add("channel" + (i + 1).ToString(), dd.ChannelName);
                dictionary.Add("units" + (i + 1).ToString(), dd.ChannelUnits);
            }*/

            double[] Values = ddc.GetValues();
            var feeds = new List<Feed>();
            while (Values != null)
            {
                Feed data = new Feed();
                data.time = DateTime.FromOADate(ddc.RealTime.ToOADate() + Values[0] / 86400);//.ToString("yyyy-MM-ddThh:mm:ssZ");
                //var itemvalues = (IDictionary<string, object>)data;

                for (int i = 1; i < Values.Length; i++)
                    if (!double.IsNaN(Values[i]))
                    {
                        data.value.Add(ddc[i-1].ChannelName, Values[i]);
                    }
                feeds.Add(data);

                Values = ddc.GetValues();
               // context.Logger.Log("feeds =" + feeds.ToString());
            }
            channels.feeds = feeds;
            return channels;
        }
    }
}
