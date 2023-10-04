using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using RXD.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaFileConvert.ExportFormats
{
    internal class XmlConverter
    {      

        public async Task<Stream> GetXSD(string bucket)
        {
            //The xsd schema must be in the same folder as the xml file
            return await LambdaGlobals.S3.GetStream(bucket, Path.Combine(LambdaGlobals.LoggerDir, "ReXConfig.xsd"));
        }
        public async Task<bool?> ConvertXMLToRxc(string bucket, string filename)
        {
            try
            {
                //Get the XML file name and bucket
                BinRXD rxd = BinRXD.Create();
                GetObjectResponse objResponse = new GetObjectResponse();
                string BucketName = bucket;
                string Key = filename;
                var request = new GetObjectRequest
                {
                    BucketName = BucketName,
                    Key = Key
                };
                objResponse = await LambdaGlobals.S3Client.GetObjectAsync(request);
                using (Stream xml = objResponse.ResponseStream)
                {
                    //Load the XSD schema needed for the XML verification
                    //XSD schema has to be in the same folder as the XML file
                    Stream xsd = await GetXSD(bucket);
                    if (xsd is null)
                    {
                        LambdaGlobals.Context?.Logger.LogInformation("XSD Schema Not Found!");
                        return null;
                    }
                    try
                    {
                        rxd.ReadXMLStructure(xml, xsd);
                    }
                    catch (Exception e)
                    {
                        LambdaGlobals.Context?.Logger.LogInformation("XML Parsing Failed: " + e.Message);
                        return null;
                    }
                    //the converted configuration file (rxc) is written in a memory stream, so it can be written back to the S3
                    MemoryStream rxc = new MemoryStream();
                    try
                    {
                        rxd.ToRXD(rxc);
                    }
                    catch (Exception e)
                    {
                        LambdaGlobals.Context?.Logger.LogInformation("RXC Conversion Failed: " + e.Message);
                        return null;
                    }
                    if (await LambdaGlobals.S3.UploadFileAsync(BucketName, Path.Combine(LambdaGlobals.FilePath, "Configuration.rxc"), rxc.ToArray()))  //The logger is looking for a file named Config.rxc to update its structure
                        LambdaGlobals.Context?.Logger.LogInformation("RXC File Uploaded Successfully");
                }

                return true;
            }
            catch (Exception e)
            {
                LambdaGlobals.Context?.Logger.LogInformation("Exception ConvertToRxc Message: " + e.Message);
                LambdaGlobals.Context?.Logger.LogInformation("Stack Trace: " + e.StackTrace);
                return false;
            }
        }
    }
}
