using InfluxShared.FileObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Cloud.Grafana
{
    public class GrafanaApi
    {
        public Dashboard dashboard { get; set; }
        public object folderId { get; set; }
        public object folderUid { get; set; }
        public string message { get; set; }
        public bool overwrite { get; set; }
        public GrafanaApi()
        {
            dashboard = new Dashboard();
        }

        public bool Export(DoubleDataCollection ddc)
        {
            Uri uri = new Uri("https://ppetkov.grafana.net/"); //new Uri("http://localhost:3000/");
            string apiKey = "";//Generated Grafana API


            
            dashboard.title = "first api";
            dashboard.tags.Add("templated");
            dashboard.timezone = "browser";

            dashboard.id = null;
            dashboard.uid = null;
            folderId = null;
            folderUid = null;
            overwrite = true;
            dashboard.panels[0].targets[0].url = "https://l76iclla6sfe2ed2xm624gn4uy0fpdze.lambda-url.eu-central-1.on.aws/?file=firstjson.json";
            dashboard.panels[0].targets[0].columns[0].selector = "time";
            for (int i = 0; i < ddc.Count; i++)
            {                
                Dashboard.Column column = new Dashboard.Column();
                column.selector = ddc[i].ChannelName;
                column.type = "number";
                dashboard.panels[0].targets[0].columns.Add(column);
            }
            using (WebClient web = new WebClient() { BaseAddress = uri.ToString() })
            {
                web.Headers[HttpRequestHeader.Accept] = "application/json";
                web.Headers[HttpRequestHeader.ContentType] = "application/json";
                web.Headers[HttpRequestHeader.Authorization] = ("Bearer " + apiKey);

                string body = JsonConvert.SerializeObject(this);
                string result = web.UploadString(uri + "api/dashboards/db", body);
                dynamic resp = JsonConvert.DeserializeObject(result);
                if (resp?.status == "success")
                {
                    int dash_id = resp.id;
                    string dash_uid = resp.uid;
                }
            }

            return true;
        }
    }

  

    
    
}
