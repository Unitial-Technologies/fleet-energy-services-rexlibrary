using RXD.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cloud.Export
{
    internal class XmlConverter
    {

        public Stream? ConvertXMLToRxc(Stream xsd, Stream xml, ILogProvider log = null)
        {
            try
            {
                //Get the XML file name and bucket
                BinRXD rxd = BinRXD.Create();
                
                    //Load the XSD schema needed for the XML verification
                    //XSD schema has to be in the same folder as the XML file
                    
                    if (xsd is null)
                    {
                        log?.Log("XSD Schema Not Found!");
                        return null;
                    }
                    try
                    {
                        rxd.ReadXMLStructure(xml, xsd);
                    }
                    catch (Exception e)
                    {
                        log?.Log("XML Parsing Failed: " + e.Message);
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
                        log?.Log("RXC Conversion Failed: " + e.Message);
                        return null;
                    }
                return rxc;
            }
            catch (Exception e)
            {
                log?.Log("Exception ConvertToRxc Message: " + e.Message);
                log?.Log("Stack Trace: " + e.StackTrace);
                return null;
            }
        }
    }
}
