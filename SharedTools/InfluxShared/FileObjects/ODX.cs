/* ------------------------------------
 * Author:  Georgi Georgiev
 * Year:    03.2024
 * ------------------------------------
 */

using InfluxShared.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Globalization;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using System.IO;

namespace InfluxShared.FileObjects
{   
    public class ODX : XML
    {
        private XmlNode odxDiagComms;
        private XmlNode odxUnitSpec;
        private XmlNode odxDiagDataDictionarySpec;
        private XmlNode odxDataObjectProp;
        private XmlNode odxStructures;
        private XmlNode odxRequests;
        private XmlNode odxPosResponses;
        private XmlNode odxTables;
        private List<ODX> List = new List<ODX>();
        private Dictionary<string, uint> IDList = new Dictionary<string, uint>();
        private string path; 

        public string FileName { get; set; }
        public XmlDocument xmlDoc = new XmlDocument();

        private void LoadTmpNodes(ODX odx, XmlNode node)
        {
            odx.odxUnitSpec = XmlNode(node, "UNIT-SPEC");
            odx.odxDiagDataDictionarySpec = XmlNode(node, "DIAG-DATA-DICTIONARY-SPEC");
            odx.odxDiagComms = XmlNode(node, "DIAG-COMMS");
            odx.odxRequests = XmlNode(node, "REQUESTS");
            odx.odxPosResponses = XmlNode(node, "POS-RESPONSES");
            odx.odxDataObjectProp = XmlNode(odxDiagDataDictionarySpec, "DATA-OBJECT-PROPS");
            odx.odxStructures = XmlNode(odxDiagDataDictionarySpec, "STRUCTURES");
            odx.odxTables = XmlNode(odxDiagDataDictionarySpec, "TABLES");
        }

        private void AddMsg(DbcMessage msg)
        {
            if ((msg.CANID == 0) || (msg.Items.Count == 0))
                return;

            foreach (DbcMessage msgthis in CANMessages)
                if (msgthis.CANID == msg.CANID)
                    return;
                                       
            CANMessages.Add(msg);
        }

        private string IdentName(XmlNode node)
        {
            string s = strContent(node, "LONG-NAME");
            if (s == "")
                s = strContent(node, "SHORT-NAME");
            return s;
        }

        private DBCValueType ValueType(XmlNode node)
        {
            string s = AttrByName(node, "BASE-DATA-TYPE");
            if (s.Contains("UINT"))
                return DBCValueType.Unsigned;
            if (s.Contains("INT"))
                return DBCValueType.Signed;
            if (s.Contains("FLOAT32"))
                return DBCValueType.IEEEFloat;
            if (s.Contains("FLOAT64"))
                return DBCValueType.IEEEDouble;

            return DBCValueType.Unsigned;
        }

        private ODX ODXFromList(string fileName)
        {
            foreach (ODX odx in List)            
                if (odx.FileName.Contains(fileName))
                    return odx;

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)            
                if (Path.GetFileName(file).Contains(fileName))
                {
                    ODX odx = new ODX();
                    odx.LoadFromFile(file, true);
                    List.Add(odx);

                    return odx;
                }            

            return null;
        }

        private void CheckExternalFiles()
        {
            List<XmlNode> nodes = new List<XmlNode>();

            bool existsInList(string fileName)
            {
                foreach (ODX odx in List)
                    if (odx.FileName.Contains(fileName))
                        return true;
                        
                return false;
            }
            
            if (odxDiagComms != null)                        
                foreach (XmlNode child in odxDiagComms.ChildNodes)
                {
                    XmlNode fcrs = XmlNode(child, "FUNCT-CLASS-REFS");
                    if (fcrs == null)
                        continue;

                    XmlNode fcr = XmlNode(fcrs, "FUNCT-CLASS-REF");
                    string docRef = AttrByName(fcr, "DOCREF");                
                    if (existsInList(docRef) || (docRef == ""))
                        continue;

                    ODX odx = ODXFromList(docRef);
                }    
            
/*            if (odxStructures != null)
                foreach (XmlNode child in odxStructures.ChildNodes)
                {
                    XmlNode prms = XmlNode(child, "PARAMS");
                    foreach (XmlNode prm in prms.ChildNodes)
                    {
                        string docRef = AttrByName(XmlNode(prm, "DOP-REF"), "DOCREF");
                        if (existsInList(docRef) || (docRef == ""))
                            continue;
                    }
                }*/
        }

        private XmlNode PARAMBySemantic(XmlNode node, string semantic, int idx = 0)
        {
            if (node == null)
                return null;

            int c = 0;
            foreach (XmlNode child in node.ChildNodes)            
                if (AttrByName(child, "SEMANTIC").ToUpper() == semantic.ToUpper())
                {
                    if (c == idx)
                        return child;
                    c++;
                }
            
            return null;
        }
        private void ExtractUnits(XmlNode node, DbcItem sig)
        {
            if (node == null)
                return;

            XmlNode unit = XmlNode(odxUnitSpec, AttrByName(node, "ID-REF"), false, "ID");
            if (unit == null)
                return;

            sig.Units = strContent(unit, "DISPLAY-NAME");
            if (sig.Units == "")
                sig.Units = strContent(unit, "SHORT-NAME");
        }

        private void ExtractCompuMethod(XmlNode node, DbcItem sig)
        {
            if (node == null)
                return;

            CompuMethodContent(node, sig);
        }

        private void ExtractPARAM(XmlNode node, DbcMessage msg, string semantic = "DATA")
        {
            int idx = 0;
            XmlNode prm = PARAMBySemantic(XmlNode(node, "PARAMS"), semantic, idx);
            while (prm != null)
            {
                ExtractDOP(prm, msg);

                idx++;
                prm = PARAMBySemantic(XmlNode(node, "PARAMS"), semantic, idx);
            }
        }

        private XmlNode ObjFromExternalFile(string fileName, string Ref)
        {
            //return null;
            ODX odx = ODXFromList(fileName);
            if (odx == null)
                return null;

            XmlNode res;
            res = odx.XmlNode(odx.odxDataObjectProp, Ref, false, "ID");
            if (res == null)
                res = odx.XmlNode(odx.odxRequests, Ref, false, "ID");
            return res;
        }

        private void ExtractTABLE(XmlNode node)
        {
            if (node == null)
                return;
            if (IDList.Count == 0)
                return;
            if (AttrByName(node, "xsi:type").ToUpper() != "TABLE-KEY")
                return;

            string Ref = AttrByName(XmlNode(node, "TABLE-REF"), "ID-REF");
            XmlNode table = XmlNode(odxTables, Ref, false, "ID");
            if (table == null)
                return;

            foreach (XmlNode child in table.ChildNodes)
            {
                if (child.Name.ToUpper() != "TABLE-ROW")
                    continue;
                string key = strContent(child, "KEY");
                Ref = AttrByName(XmlNode(child, "STRUCTURE-REF"), "ID-REF");
                XmlNode struc = XmlNode(odxStructures, Ref, false, "ID");

                foreach (KeyValuePair<string, uint> pair in IDList)                
                    if (pair.Key == key)
                    {
                        DbcMessage msg = new DbcMessage();
                        msg.Name = key;
                        msg.CANID = pair.Value;
                        ExtractPARAM(struc, msg);
                        AddMsg(msg);                        
                    }                
            }
        }

        private void ExtractEOPF(XmlNode node, DbcMessage msg)
        {
            if(node == null) 
                return;
            if (node.Name.ToUpper() != "END-OF-PDU-FIELD")
                return;

            string Ref = AttrByName(XmlNode(node, "BASIC-STRUCTURE-REF"), "ID-REF");
            XmlNode struc = XmlNode(odxStructures, Ref, false, "ID");
            ExtractPARAM(struc, msg);
        }

        private void ExtractDOP(XmlNode node, DbcMessage msg)
        {
            if (node == null)
                return;

            XmlNode Ref = XmlNode(node, "DOP-REF");
            string idRef = AttrByName(Ref, "ID-REF");
            string docRef = AttrByName(Ref, "DOCREF");           
            XmlNode dop = XmlNode(odxDiagDataDictionarySpec, idRef, false, "ID");
            if (dop == null)
                dop = ObjFromExternalFile(docRef, idRef);
            
            if (dop != null)
                if (dop.Name.ToUpper() == "END-OF-PDU-FIELD")
                {
                    ExtractEOPF(dop, msg);
                    return;               
            }

            XmlNode struc = XmlNode(odxStructures, idRef, false, "ID");
            if (struc != null)
            {
                ExtractPARAM(struc, msg);
                return;
            }

            if (dop == null)
                return;

            ushort bytePos = (ushort)(uintContent(node, "BYTE-POSITION") - 3);

            DbcItem sig = new DbcItem();
            sig.Ident = msg.CANID;
            sig.Name = IdentName(node);
            sig.Type = DBCSignalType.ModeDependent; // !
            sig.Comment = strContent(dop, "DESC");
            sig.StartBit = (ushort)(bytePos * 8); // !

            XmlNode dct = XmlNode(dop, "DIAG-CODED-TYPE");
            sig.BitCount = (ushort)uintContent(dct, "BIT-LENGTH");
            sig.ByteOrder = AttrByName(dct, "IS-HIGHLOW-BYTE-ORDER1") == "true" ? DBCByteOrder.Motorola : DBCByteOrder.Intel;
            sig.ValueType = ValueType(XmlNode(dop, "PHYSICAL-TYPE"));

            ExtractCompuMethod(XmlNode(dop, "COMPU-METHOD"), sig);

            XmlNode ic = XmlNode(dop, "INTERNAL-CONSTR");
            sig.MinValue = doubleContent(ic, "LOWER-LIMIT1", sig.MinValue);
            sig.MaxValue = doubleContent(ic, "UPPER-LIMIT", sig.MaxValue);

            ExtractUnits(XmlNode(dop, "UNIT-REF"), sig);

            msg.Items.Add(sig);
            byte dlc = (byte)(sig.BitCount / 8);
            msg.DLC = (byte)(msg.DLC + dlc);           
        }

        private void ExtractREQUEST(XmlNode node, DbcMessage msg, uint id = 0)
        {
            XmlNode reqRef = XmlNode(node, "REQUEST-REF");
            string Ref = AttrByName(reqRef, "ID-REF");
            string idRef = AttrByName(reqRef, "ID-REF");
            string docRef = AttrByName(reqRef, "DOCREF");

            XmlNode req = XmlNode(odxRequests, Ref, false, "ID");
            if (req == null)
                req = ObjFromExternalFile(docRef, idRef);

            XmlNode prm = PARAMBySemantic(XmlNode(req, "PARAMS"), "SERVICE-ID");
            if (uintContent(prm, "CODED-VALUE") != id)
                return;

            prm = PARAMBySemantic(XmlNode(req, "PARAMS"), "ID");

            msg.CANID = uintContent(prm, "CODED-VALUE");

            // table represented ID's
            if (prm == null)
                prm = PARAMBySemantic(XmlNode(req, "PARAMS"), "DATA-ID");
            if (prm == null)
                return;

            IDList.Clear();
            Ref = AttrByName(XmlNode(prm, "DOP-SNREF"), "SHORT-NAME");
            XmlNode dop = XmlNode(odxDataObjectProp, Ref, false, "ID");            
            if (strContent(XmlNode(dop, "COMPU-METHOD"), "CATEGORY").ToUpper() != "TEXTTABLE")
                return;
            XmlNode citp = XmlNode(XmlNode(dop, "COMPU-METHOD"), "COMPU-INTERNAL-TO-PHYS");
            citp = XmlNode(citp, "COMPU-SCALES");
            foreach (XmlNode child in citp.ChildNodes)
            {               
                uint ID = uintContent(child, "LOWER-LIMIT");
                XmlNode cc = XmlNode(child, "COMPU-CONST");
                string name = strContent(cc, "VT");
                IDList.Add(name, ID);
            }
        }

        private void ExtractPOS_RESP(XmlNode node, DbcMessage msg, uint id = 0)
        {
            XmlNode prr = XmlNode(node, "POS-RESPONSE-REFS");
            string Ref = AttrByName(XmlNode(prr, "POS-RESPONSE-REF"), "ID-REF");

            XmlNode resp = XmlNode(odxPosResponses, Ref, false, "ID");

            XmlNode prm = PARAMBySemantic(XmlNode(resp, "PARAMS"), "DATA-ID");
            if (prm != null)
                ExtractTABLE(prm);

            ExtractPARAM(resp, msg);
        }

        private void ExtractPOS_RESP(XmlNode node, uint id)
        {
            if (IDList.Count == 0) 
                return;

            XmlNode prr = XmlNode(node, "POS-RESPONSE-REFS");
            string Ref = AttrByName(XmlNode(prr, "POS-RESPONSE-REF"), "ID-REF");
            XmlNode resp = XmlNode(odxPosResponses, Ref, false, "ID");
        }

        private void CreateServiceByID(XmlNode node, uint id)
        {
            if (node.Name != "DIAG-SERVICE")
                return;
            
            string shname = strContent(node, "SHORT-NAME"); ///

            DbcMessage msg = new DbcMessage();
            msg.Name = IdentName(node);
            ExtractREQUEST(node, msg, id);

            ExtractPOS_RESP(node, msg, id);
            if (msg.CANID != 0)                                                                     
                AddMsg(msg);            
        }

        public ODX()
        {
        }
        public void LoadFromFile(string filename, bool addToList = false)
        {
            // addToList = true чете ODX файл, за който има препратка в основния файл

            void DoProcess(ODX odx)
            {
                odx.LoadTmpNodes(this, odx.xmlDoc);
                if (odx.odxDiagComms != null)
                    foreach (XmlNode child in odx.odxDiagComms.ChildNodes)
                    {                    
                        CreateServiceByID(child, 0x22);
                    }
            }

            path = Path.GetDirectoryName(filename) + "\\";
            FileName = Path.GetFileName(filename);
            CANMessages.Clear();

            xmlDoc = new XmlDocument();
            xmlDoc.Load(filename);

            XmlNode node = xmlDoc.DocumentElement.FirstChild;

            // MDX file
            if ((xmlDoc.DocumentElement.Name == "MDX") || (xmlDoc.DocumentElement.Name == "GDX"))
            {
                ExtractMDX(xmlDoc.DocumentElement);
                return;
            }

            LoadTmpNodes(this, node);

            if (addToList)
                return;

            //CheckExternalFiles();
            DoProcess(this);
            foreach (ODX odx in List)
                DoProcess(odx);                    
        }

        // MDX (GDX) support
        public void ExtractMDX(XmlNode node)
        {
            if (node == null)
                return;

            XmlNode dataID = XmlNode(node, "DATA_IDENTIFIERS");
            ExtractDID(dataID);
        }

        public void ExtractDID(XmlNode node)
        {
            if (node == null)
                return;

            if (node.ChildNodes.Count == 0)
                return;
           
            foreach (XmlNode child in node.ChildNodes)
            {
                DbcMessage msg = new DbcMessage();                
                msg.Name = strContent(child, "NAME");
                msg.CANID = hexContent(child, "NUMBER");
                msg.DLC = (byte)uintContent(child, "BYTE_SIZE");
                msg.Comment = strContent(child, "DESCRIPTION");

                ExtractSubField(child, msg);

                AddMsg(msg);
            }
        }

        public void ExtractSubField(XmlNode node, DbcMessage msg)
        {
            if (node == null) 
                return;

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name.ToUpper() != "SUB_FIELD")
                    continue;

                DbcItem sig = new DbcItem();
                sig.Ident = msg.CANID;
                sig.Name = strContent(child, "NAME");
                if (sig.Name == "")
                    sig.Name = msg.Name;
                
                string lsb = strContent(child, "LEAST_SIG_BIT");
                string msb = strContent(child, "MOST_SIG_BIT");
                sig.ByteOrder = (lsb.Contains("-") || msb.Contains("-")) ? DBCByteOrder.Motorola : DBCByteOrder.Intel;  // надявам се да е вярно               

                sig.StartBit = (ushort)uintContent(child, "LEAST_SIG_BIT");
                if (sig.ByteOrder == DBCByteOrder.Intel)                   
                    sig.BitCount = (ushort)(uintContent(child, "MOST_SIG_BIT") - sig.StartBit + 1);                
                else                
                    sig.BitCount = (ushort)(sig.StartBit - uintContent(child, "MOST_SIG_BIT") + 1);               

                ExtractDataDefinition(child, sig);

                if (sig.Conversion.Type == ConversionType.None)    
                    return;

                msg.Items.Add(sig);
            }
        }

        private void ExtractDataDefinition(XmlNode node, DbcItem sig)
        {
            if (node == null) 
                return;

            XmlNode dataDef = XmlNode(node, "DATA_DEFINITION");
            if (dataDef == null) 
                return;

            sig.Comment = strContent(dataDef, "DESCRIPTION");
            string dataType = strContent(dataDef, "DATA_TYPE").Replace(" ", string.Empty);
            sig.Conversion.Type = ConversionType.Formula;
            if (dataType.ToLower() == "enumerated") 
                sig.Conversion.Type = ConversionType.FormulaAndTableVerbal;
            // DATA_TYPE ascii, bcd, bytes не са реализирани
            if (dataType.ToLower() == "unknown")    // има и такъв, не знам какво да го правя, затова ...
                sig.Conversion.Type = ConversionType.None;

            sig.ValueType = DBCValueType.Signed;
            if (dataType.ToLower() == "unsigned")
                sig.ValueType = DBCValueType.Unsigned;

            sig.Conversion.Formula.CoeffB = 1;
            sig.Conversion.Formula.CoeffC = 0;

            ExtractEnumParams(dataDef, sig);
            ExtractNumericParams(dataDef, sig);
        }

        private void ExtractEnumParams(XmlNode node, DbcItem sig)
        {
            if (node == null)
                return;

            XmlNode enumParams = XmlNode(node, "ENUMERATED_PARAMETERS");
            if (enumParams == null) 
                return;

            foreach (XmlNode child in enumParams.ChildNodes)
            {
                if (child.Name.ToUpper() != "ENUM_MEMBER")
                    continue;

                uint val = strContent(child, "ENUM_VALUE").Contains("0x") ? hexContent(child, "ENUM_VALUE") : uintContent(child, "ENUM_VALUE");
                string desc = strContent(child, "DESCRIPTION");
                sig.Conversion.TableVerbal.Pairs.Add(val, desc);
            }
        }

        private void ExtractNumericParams(XmlNode node, DbcItem sig)
        {
            if (node == null)
                return;

            XmlNode numParams = XmlNode(node, "NUMERIC_PARAMETERS");
            if (numParams == null)
                return;

            sig.Units = strContent(numParams, "UNITS");
            sig.MinValue = doubleContent(numParams, "RANGE_LOW");
            sig.MaxValue = doubleContent(numParams, "RANGE_HIGH");

            XmlNode resolution = XmlNode(numParams, "RESOLUTION");
            sig.Conversion.Formula.CoeffB = doubleContent(resolution, "RESOLUTION", 1);
            sig.Conversion.Formula.CoeffC = doubleContent(numParams, "OFFSET");
            
            // ailetnative way to calc CoeffB            
            if ((AttrByName(resolution, "numerator") != "") && (AttrByName(resolution, "denominator") != ""))
            {
                double numerator = ConvertToDouble(AttrByName(resolution, "numerator"), 1);
                double denominator = ConvertToDouble(AttrByName(resolution, "denominator"), 1);
                sig.Conversion.Formula.CoeffB = numerator / denominator;
            }
        }
    }
}