using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Serialization;

namespace InfluxShared.Helpers
{
    public static class TripleDESHelper
    {
        public static bool EncryptToXml(this TripleDES CryptoAlgorithm, object obj, string path)
        {
            try
            {
                // Serialize object to xml
                XmlDocument xmlFile = new XmlDocument();
                using (XmlWriter writer = xmlFile.CreateNavigator().AppendChild())
                    new XmlSerializer(obj.GetType()).Serialize(writer, obj);

                // If the element was not found, throw an exception.
                if (!(xmlFile.GetElementsByTagName(obj.GetType().Name)[0] is XmlElement inputElement))
                    throw new Exception("The element was not found.");

                // Create a new EncryptedXml object.
                EncryptedXml exml = new EncryptedXml(xmlFile);

                // Encrypt the element using the symmetric key.
                byte[] rgbOutput = exml.EncryptData(inputElement, CryptoAlgorithm, false);

                // Create an EncryptedData object and populate it.
                EncryptedData ed = new EncryptedData
                {
                    // Specify the namespace URI for XML encryption elements.
                    Type = EncryptedXml.XmlEncElementUrl,

                    // Specify the namespace URI for the TrippleDES algorithm.
                    EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncTripleDESUrl),

                    // Create a CipherData element.
                    CipherData = new CipherData
                    {
                        // Set the CipherData element to the value of the encrypted XML element.
                        CipherValue = rgbOutput
                    }
                };

                // Replace the plaintext XML elemnt with an EncryptedData element.
                EncryptedXml.ReplaceElement(inputElement, ed, false);

                xmlFile.Save(path);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                CryptoAlgorithm.Clear();
            }
        }

        public static T DecryptFromXml<T>(this TripleDES CryptoAlgorithm, string path)
        {
            try
            {
                XmlDocument xmlFile = new XmlDocument();
                // Load XML data
                xmlFile.Load(path);

                // If the EncryptedData element was not found, throw an exception.
                if (!(xmlFile.GetElementsByTagName("EncryptedData")[0] is XmlElement encryptedElement))
                    throw new Exception("The EncryptedData element was not found.");

                // Create an EncryptedData object and populate it.
                EncryptedData ed = new EncryptedData();
                ed.LoadXml(encryptedElement);

                // Create a new EncryptedXml object.
                EncryptedXml exml = new EncryptedXml();

                // Decrypt the element using the symmetric key.
                byte[] rgbOutput = exml.DecryptData(ed, CryptoAlgorithm);

                // Replace the encryptedData element with the plaintext XML elemnt.
                exml.ReplaceData(encryptedElement, rgbOutput);

                return (T)new XmlSerializer(typeof(T)).Deserialize(new XmlNodeReader(xmlFile));
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return default;
            }
            finally
            {
                CryptoAlgorithm.Clear();
            }
        }

    }
}
