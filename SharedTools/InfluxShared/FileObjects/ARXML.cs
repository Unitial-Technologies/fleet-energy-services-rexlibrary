/* ------------------------------------
 * Author:  Georgi Georgiev
 * Year:    01.2024
 * ------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace InfluxShared.FileObjects
{
    public class ARXML : XML
    {
        private XmlNode arxmlFrames;
        private XmlNode arxmlPDUs;
        private XmlNode arxmlSignals;
        private XmlNode arxmlCompuMethods;
        private XmlNode arxmlUnits;
        private XmlNode arxmlBaseTypes;
        private XmlNode arxmlFrameTriggerings;

        private XmlNode ELEMENTSNode(XmlNode node)
        {
            return XmlNode(node, "ELEMENTS");
        }

        private string attrContent(XmlNode node, string text)
        {
            if (node == null)
                return "";

            foreach (XmlNode child in node.Attributes)
                if (child.Name.ToUpper() == text.ToUpper())
                    return child.Value;

            return "";
        }

        private void CreateBaseType(XmlNode Ref, DbcItem sig)
        {
            XmlNode node = NodeFromRef(arxmlBaseTypes, Ref);
            if (node == null)
                return;

            if (strContent(node, "NATIVE-DECLARATION").ToString().Contains("sint"))            
                sig.ValueType = DBCValueType.Signed;
        }

        private void CreateUnits(XmlNode Ref, DbcItem sig)
        {
            XmlNode node = NodeFromRef(arxmlUnits, Ref);
            if (node == null)
                return;

            sig.Units = strContent(node, "DISPLAY-NAME");
        }

        private void CreateCompuMethod(XmlNode Ref, DbcItem sig)
        {
            XmlNode node = NodeFromRef(arxmlCompuMethods, Ref);
            if (node == null)
                return;

            CreateUnits(XmlNode(node, "UNIT-REF"), sig);

            CompuMethodContent(node, sig);
        }

        private void CreateCANSignals(XmlNode Ref, DbcItem sig)
        {
            XmlNode node = NodeFromRef(arxmlSignals, Ref);
            if (node == null)
                return;

            sig.BitCount = (ushort)uintContent(node, "LENGTH");
            sig.Comment = strContent(XmlNode(node, "DESC"), "L-2");

            node = XmlNode(node, "NETWORK-REPRESENTATION-PROPS");
            node = XmlNode(node, "SW-DATA-DEF-PROPS-VARIANTS");
            node = XmlNode(node, "SW-DATA-DEF-PROPS-CONDITIONAL");
            CreateCompuMethod(XmlNode(node, "COMPU-METHOD-REF"), sig);
            CreateBaseType(XmlNode(node, "BASE-TYPE-REF"), sig);
        }

        private void CreateMUXCANPdus(XmlNode Ref, DbcMessage msg)
        {
            XmlNode node = NodeFromRef(arxmlPDUs, Ref);
            if (node == null)
                return;

            msg.CANID = uintContent(XmlNode(node, "CONTAINED-I-PDU-PROPS"), "HEADER-ID-LONG-HEADER");

            DbcItem sigS = new DbcItem();
            sigS.Ident = msg.CANID;
            sigS.Name = "Service";
            sigS.ByteOrder = strContent(node, "SELECTOR-FIELD-BYTE-ORDER") == "MOST-SIGNIFICANT-BYTE-LAST" ? DBCByteOrder.Intel : DBCByteOrder.Motorola;
            sigS.BitCount = (ushort)uintContent(node, "SELECTOR-FIELD-LENGTH");
            sigS.StartBit = (ushort)uintContent(node, "SELECTOR-FIELD-START-POSITION");
            sigS.Conversion.Type = ConversionType.FormulaAndTableVerbal;
            sigS.Type = DBCSignalType.Mode;

            XmlNode node1 = XmlNode(node, "DYNAMIC-PARTS");
            node1 = XmlNode(node1, "DYNAMIC-PART");

            XmlNode node2 = XmlNode(node1, "SEGMENT-POSITIONS");
            node2 = XmlNode(node2, "SEGMENT-POSITION");
            var segPos = uintContent(node2, "SEGMENT-POSITION");

            node2 = XmlNode(node1, "DYNAMIC-PART-ALTERNATIVES");
            if (node2 != null)
            {
                foreach (XmlNode child in node2.ChildNodes)
                {
                    Ref = XmlNode(child, "I-PDU-REF");
                    CreateCANPdus(Ref, msg);

                    // change some prop of added Mode depended signals
                    var val = uintContent(child, "SELECTOR-FIELD-CODE");
                    foreach (var sig in msg.Items)
                        if (sig.Type == DBCSignalType.Standard)
                        {
                            sig.Type = DBCSignalType.ModeDependent;
                            sig.StartBit += (ushort)(segPos + 1);
                            sig.Mode = (byte)val;
                        }
                   
                    sigS.Conversion.TableVerbal.Pairs.Add(val, RefFromFullRef(Ref));
                }
            }

            msg.Items.Add(sigS);
        }

        private void CreateCANPdus(XmlNode Ref, DbcMessage msg)
        {  
            XmlNode node = NodeFromRef(arxmlPDUs, Ref);
            if (node == null)
                return;

            XmlNode node1 = XmlNode(node, "I-SIGNAL-TO-PDU-MAPPINGS");
            if (node1 != null)
                foreach (XmlNode child in node1.ChildNodes)
                {
                    DbcItem sig = new DbcItem();
                    sig.Ident = msg.CANID;
                    sig.Name = strContent(child, "SHORT-NAME");
                    sig.ByteOrder = strContent(child, "PACKING-BYTE-ORDER") == "MOST-SIGNIFICANT-BYTE-LAST" ? DBCByteOrder.Intel : DBCByteOrder.Motorola;
                    sig.StartBit = (ushort)uintContent(child, "START-POSITION");
                    sig.Type = DBCSignalType.Standard;                    

                    CreateCANSignals(XmlNode(child, "I-SIGNAL-REF"), sig);

                    msg.Items.Add(sig);
                }

            // node седържа секция I-PDU-TIMING-SPECIFICATIONS, която не се обработва
        }

        private void CreateCANFrames(XmlNode Ref, DbcMessage msg)
        {
            XmlNode node = NodeFromRef(arxmlFrames, Ref);
            if (node == null)
                return;

            msg.Name = strContent(node, "SHORT-NAME");
            msg.DLC = (byte)uintContent(node, "FRAME-LENGTH");

            node = XmlNode(node, "PDU-TO-FRAME-MAPPINGS");
            Ref = XmlNode(node, "PDU-REF");
            if (attrContent(Ref, "DEST").Contains("MULTIPLEXED"))
                CreateMUXCANPdus(Ref, msg);
            else
                CreateCANPdus(Ref, msg);
        }

        private void CreateCANFrameTriggerings(XmlNode node)
        {
            DbcMessage msg = new DbcMessage();

            msg.Name = strContent(node, "SHORT-NAME");
            msg.MsgType = strContent(node, "CAN-ADDRESSING-MODE") == "STANDARD" ? DBCMessageType.Standard : DBCMessageType.Extended;
            CreateCANFrames(XmlNode(node, "FRAME-REF"), msg);
            msg.CANID = (uint)uintContent(node, "IDENTIFIER");
            
            if (msg.CANID != 0/*)//*/ && msg.Items.Count > 0)
                CANMessages.Add(msg);
        }

        public ARXML()
        {
            //CANMessages = new List<DbcMessage>();
        }

        public void LoadFromFile(string filename)
        {
            FileName = filename;
            CANMessages.Clear();

            XmlDocument arxmlDoc = new XmlDocument();
            arxmlDoc.Load(filename);

            XmlNode node = null;
            foreach (XmlNode child in arxmlDoc.DocumentElement.ChildNodes)
                if (child.Name == "AR-PACKAGES")
                    node = child;
                    
            arxmlFrames = ELEMENTSNode(XmlNode(node, "Frames", false));
            arxmlPDUs = ELEMENTSNode(XmlNode(node, "PDUs", false));
            arxmlSignals = ELEMENTSNode(XmlNode(node, "Signals", false));
            arxmlCompuMethods = ELEMENTSNode(XmlNode(node, "CompuMethods", false));
            arxmlUnits = ELEMENTSNode(XmlNode(node, "Units", false));
            arxmlBaseTypes = ELEMENTSNode(XmlNode(node, "BaseTypes", false));
            arxmlFrameTriggerings = XmlNode(node, "FRAME-TRIGGERINGS");

            if (arxmlFrameTriggerings == null)
                return;
            foreach (XmlNode child in arxmlFrameTriggerings.ChildNodes)
                if (child.Name == "CAN-FRAME-TRIGGERING")
                    CreateCANFrameTriggerings(child);

        }
    }
}