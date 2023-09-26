using InfluxShared.Generic;
using RXD.Blocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace RXD.Base
{
    public class XmlHandler : IDisposable
    {
        public static readonly string Extension = ".xml";
        public static readonly string Filter = "ReX XML Structure (*.xml)|*.xml";
        static readonly XNamespace ns = @"http://www.influxtechnology.com/xml/ReXgen";
        static readonly XNamespace xsi = @"http://www.w3.org/2001/XMLSchema-instance";
        static readonly string xsiLocation = "http://www.influxtechnology.com/xml/ReXgen ReXConfig.xsd";

        static string xsdLocalPath = AppDomain.CurrentDomain.BaseDirectory + "ReXConfig.xsd";

        static string rootNodeName = "REXGENCONFIG";
        static string blocksNodeName = "BLOCKS";
        static string groupsSuffix = "_LIST";

        XmlSchema schema = null;

        readonly string xmlFileName;
        XDocument xmlFile;
        public XElement rootNode;
        public XElement configNode;
        public XElement blocksNode;

        ValidationEventHandler xmlValidationHandler = null;

        public XmlHandler(string xmlPath)
        {
            xmlFileName = xmlPath;
            if (File.Exists(xmlFileName))
                xmlFile = XDocument.Load(xmlFileName);

            // Create validation handler
            xmlValidationHandler = new ValidationEventHandler((object sender, ValidationEventArgs args) => throw new Exception(args.Message));
            //xmlValidationHandler = new ValidationEventHandler((object sender, ValidationEventArgs args) => { });

            // Read XSD schema from local file
            XmlTextReader schemaReader = new XmlTextReader(xsdLocalPath);
            schema = XmlSchema.Read(schemaReader, xmlValidationHandler);
        }

        public XmlHandler(Stream xmlData, Stream xsdData)
        {
            xmlFile = XDocument.Load(xmlData);

            // Create validation handler
            xmlValidationHandler = new ValidationEventHandler((object sender, ValidationEventArgs args) => throw new Exception(args.Message));
            //xmlValidationHandler = new ValidationEventHandler((object sender, ValidationEventArgs args) => { });

            // Read XSD schema from stream
            schema = XmlSchema.Read(xsdData, xmlValidationHandler);
        }

        #region Writing XML 
        public void CreateRoot(string Version, XElement[] ConfigNodes)
        {
            rootNode =
                new XElement(ns + rootNodeName,
                new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                new XAttribute(xsi + "schemaLocation", xsiLocation),
                NewElement("VERSION-TYPE", Version),
                ConfigNodes,
                blocksNode = NewElement(blocksNodeName)
            );
            xmlFile = new XDocument(rootNode);
        }

        public XElement NewElement(string Name, params object[] content)
        {
            return new XElement(ns + Name, content);
        }

        public XElement AddGroupNode(string Name)
        {
            XElement node = NewElement(Name.ToUpper() + groupsSuffix);
            blocksNode.Add(node);
            return node;
        }

        public XmlSchemaComplexType xsdNodeType(XElement node)
        {
            XmlSchemaElement xsdElement = schema.Items.OfType<XmlSchemaElement>().Where(x => x.Name == node.Name.LocalName).FirstOrDefault();
            if (xsdElement is null)
                return null;

            return schema.Items.OfType<XmlSchemaComplexType>().Where(x => x.Name == xsdElement.Name).FirstOrDefault();
        }

        public XmlSchemaElement xsdObjectProperty(XmlSchemaComplexType xsdType, string PropName)
        {
            if (xsdType is null)
                return null;

            if (!xsdType.Particle.GetType().IsSubclassOf(typeof(XmlSchemaGroupBase)))
                return null;

            return (xsdType.Particle as XmlSchemaGroupBase).Items.OfType<XmlSchemaElement>().Where(x => x.Name == PropName).FirstOrDefault();
        }

        public XmlSchemaAttribute xsdObjectAttribute(XmlSchemaComplexType xsdType, string PropName)
        {
            if (xsdType is null)
                return null;

            if (!xsdType.Particle.GetType().IsSubclassOf(typeof(XmlSchemaGroupBase)))
                return null;

            var attr = (xsdType.Particle as XmlSchemaGroupBase).Items.OfType<XmlSchemaAttribute>().Where(x => x.Name == PropName).FirstOrDefault();
            if (attr is not null)
                return attr;

            return null;
        }

        public void Save()
        {
            if (xmlFile != null)
                xmlFile.Save(xmlFileName);
        }
        #endregion

        public void Dispose()
        {
        }

        #region Reading XML
        public bool TryLoadXML(out string xsdError)
        {
            try
            {
                // Create SchemaSet for validating
                XmlSchemaSet xmlSchemas = new XmlSchemaSet();
                xmlSchemas.Add(schema);

                // Load XML and Validate using XSD schema
                //xmlFile = XDocument.Load(xmlFileName);
                xmlFile.Validate(xmlSchemas, xmlValidationHandler);

                // Read XML contents
                rootNode = xmlFile.Element(ns + rootNodeName);
                blocksNode = rootNode.Element(ns + blocksNodeName);
                configNode = rootNode.Element(ns + BlockType.Config.ToString().ToUpper());

                xsdError = "";
                return true;
            }
            catch (Exception e)
            {
                xsdError = e.Message;
                return false;
            }
        }

        public static XElement Child(XElement node, string ChildName)
        {
            return node.Element(ns + ChildName);
        }

        public static IEnumerable<XElement> Childs(XElement node, string ChildName)
        {
            return node.Elements(ns + ChildName);
        }

        #endregion

        #region Converters
        public string ToHexBytes(object obj)
        {
            return BitConverter.ToString(Bytes.ArrayToBytes(obj, (obj as Array).Length)).Replace("-", string.Empty);
        }
        #endregion
    }
}
