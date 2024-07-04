using InfluxShared.Generic;
using InfluxShared.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace InfluxShared.FileObjects
{

    public class OutputMessage
    {
        public UInt32 CanID { get; set; } = 1;

        public DBCMessageType CanMsgType { get; set; }

        public string CanType
        {
            get => CanMsgType.ToDisplayName();
            set
            {
                for (DBCMessageType mt = DBCMessageType.Standard; mt <= DBCMessageType.CanFDExtended; mt++)
                    if (mt.ToDisplayName().Equals(value))
                    {
                        CanMsgType = mt;
                        break;
                    }
            }
        }

        public bool BRS { get; set; }

        public CanFDMessageType CanFDOption =>
            (CanMsgType == DBCMessageType.Standard || CanMsgType == DBCMessageType.Extended)
            ? CanFDMessageType.NORMAL_CAN
            : BRS ? CanFDMessageType.FD_FAST_CAN : CanFDMessageType.FD_CAN;

        public byte DLC { get; set; }

        public string strDLC
        {
            get => DLC.ToString();
            set
            {
                if (int.TryParse(value, out int dlc))
                {
                    DLC = (byte)dlc;
                    byte[] data = Data;
                    Array.Resize(ref data, dlc);
                    Data = data;
                }
            }
        }

        public byte[] Data { get; set; }

        public string strData
        {
            get => ArrayToString(Data);
            set
            {
                string datastr = value.Replace(" ", "").Replace(Environment.NewLine, "").PadRight(DLC * 2, '0').Substring(0, DLC * 2);
                Data = Bytes.FromHexBinary(datastr);
            }
        }

        public bool Can0 { get; set; }
        public bool Can1 { get; set; }
        public bool Can2 { get; set; }
        public bool Can3 { get; set; }

        public static string ArrayToString(object obj)
        {
            string data = "";
            byte[] tmp = Bytes.ArrayToBytes(obj, (obj as Array).Length);
            if (tmp is not null)
                for (int i = 0; i < tmp.Length; i += 8)
                    data += BitConverter.ToString(tmp.Slice(i, 8)).Replace("-", " ") + Environment.NewLine;
            return data.Trim();
        }

        public UInt32 Period { get; set; }
        public UInt32 Delay { get; set; }
        public UInt16 UID { get; set; }
        public UInt16 NextOutputID { get; set; }
        public bool Linked { get; set; }
        public bool IsChild { get; set; }
        public bool LogTx { get; set; }
        public uint TxIdent { get; set; }
        public List<Object> LinkedParameters { get; set; } = new();

        public OutputMessage()
        {
            Period = 100;
            DLC = 8;
            Data = new byte[DLC];
        }



    }

    public static class OutputMessageListHelper
    {
        public static bool LoadFromCsv(this List<OutputMessage> messages, string csvFile)
        {
            OutputMessage canMsg;
            OutputMessage lastCanMsg = null;
            int rowCounter = 1;
            try
            {
                using (StreamReader reader = new StreamReader(csvFile))
                {
                    messages.Clear();
                    string row = reader.ReadLine();
                    rowCounter++;
                    while (row != "")
                    {
                        row = reader.ReadLine();
                        if (row == null)
                            break;
                        string[] items = row.Split(',');
                        if (items.Length >= 10)
                        {
                            uint canId = (uint)Integers.StrToIntDef(items[0], 0);
                            // canMsg = bus.CanMessages.Where(x => x.CanID == canId).FirstOrDefault();
                            // if (canMsg == null)
                            // {
                            canMsg = new OutputMessage();
                            messages.Add(canMsg);
                            //  }
                            canMsg.CanID = canId;
                            canMsg.Linked = items[1] != "";
                            if (canMsg.Linked)
                            {
                                canMsg.IsChild = true;
                                if (!lastCanMsg.Linked)
                                    lastCanMsg.Linked = true;
                            }
                            canMsg.Period = (uint)Integers.StrToIntDef(items[2], 100);
                            canMsg.Delay = (uint)Integers.StrToIntDef(items[3], 0);
                            canMsg.Can0 = items[4] != "";
                            canMsg.Can1 = items[5] != "";
                            canMsg.Can2 = items[6] != "";
                            canMsg.Can3 = items[7] != "";
                            if (!canMsg.Can1 && !canMsg.Can2 && !canMsg.Can3)
                                canMsg.Can0 = true;
                            canMsg.CanMsgType = (DBCMessageType)Integers.StrToIntDef(items[8], 0); ;
                            canMsg.BRS = items[9] != "";
                            canMsg.DLC = (byte)Integers.StrToIntDef(items[10], 8); ;
                            string[] data = items[11].Split(' ');
                            canMsg.Data = new byte[canMsg.DLC];
                            for (int i = 0; i < data.Length; i++)
                            {
                                canMsg.Data[i] = (byte)Integers.StrToIntDef("0x" + data[i], 0);
                            }
                            lastCanMsg = canMsg;
                        }
                    }
                }
                return true;
            }
            catch (Exception exc)
            {
                //LastError = $"Error parsing csv row {rowCounter} Error: {exc.Message}";
                return false;
            }
        }

        public static bool LoadFromModuleXml(this List<OutputMessage> messages, ModuleXml modXml)
        {
            OutputMessage canMsg;
            try
            {
                string groupID = "";
                //messages.Clear();
                foreach (var xmlMsg in modXml.PollingItemList.Items.GroupBy(x => x.Ident).Select(group => group.First()).OrderBy(x => x.Order))
                {
                    canMsg = new OutputMessage();
                    messages.Add(canMsg);
                    canMsg.Delay = (uint)xmlMsg.Delay;
                    canMsg.CanID = xmlMsg.TxIdent;
                    canMsg.IsChild = true;
                    if (groupID != $"CAN{modXml.Config?.CanBus}_{xmlMsg.TxIdent}")
                    {
                        canMsg.IsChild = false;
                        groupID = $"CAN{modXml.Config?.CanBus}_{xmlMsg.TxIdent}";
                    }
                    if (modXml.Config?.CanBus == 1)
                        canMsg.Can1 = true;
                    else if (modXml.Config?.CanBus == 2)
                        canMsg.Can2 = true;
                    else if (modXml.Config?.CanBus == 3)
                        canMsg.Can3 = true;
                    else
                        canMsg.Can0 = true;
                    canMsg.Data = new byte[] { 3, 0x22, (byte)(xmlMsg.Ident >> 8), (byte)xmlMsg.Ident };
                    canMsg.DLC = 4;// (byte)(xmlMsg.Data.Length);
                    canMsg.BRS = false;
                    canMsg.Linked = true;
                    canMsg.TxIdent = xmlMsg.TxIdent;
                }

                return true;
            }
            catch (Exception exc)
            {
                //LastError = $"Error parsing csv row {rowCounter} Error: {exc.Message}";
                return false;
            }
        }

        public static void SaveToCsv(this List<OutputMessage> messages, string csvFile)
        {
            using (FileStream fs = new FileStream(csvFile, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    string row = "Ident,Linked,Period,Delay,CAN 0,CAN 1,CAN 2,CAN 3,Type,BRS,DLC,Data" + Environment.NewLine;
                    sw.Write(row);
                    foreach (var msg in messages)
                    {
                        row = "0x" + msg.CanID.ToString("X2") + ',';
                        row += msg.Linked && msg.IsChild ? "1," : ',';
                        row += msg.Period > 0 && !msg.IsChild ? msg.Period.ToString() + ',' : ',';
                        row += msg.Delay > 0 && msg.IsChild ? msg.Delay.ToString() + ',' : ',';
                        row += msg.Can0 && !msg.IsChild ? "1," : ',';
                        row += msg.Can1 && !msg.IsChild ? "1," : ',';
                        row += msg.Can2 && !msg.IsChild ? "1," : ',';
                        row += msg.Can3 && !msg.IsChild ? "1," : ',';
                        row += msg.CanMsgType != DBCMessageType.Standard ? ((byte)msg.CanMsgType).ToString() + "," : ',';
                        row += msg.BRS ? "1," : ',';
                        row += msg.DLC.ToString() + ',';
                        for (int i = 0; i < msg.Data.Length; i++)
                        {
                            row += msg.Data[i].ToString("X2") + ' ';
                        }
                        row = row.Trim() + Environment.NewLine;
                        sw.Write(row);
                    }
                }
            }
        }


    }

}
