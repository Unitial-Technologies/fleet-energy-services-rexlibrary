using Influx.Shared.Helpers;
using RXD.Base;
using RXD.Blocks;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RxLibrary
{
    public static class RxLib
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct CanBusInfo
        {
            public byte channel;
            public UInt16 CanFrame;
            public UInt16 ErrorFrame;
        }

        static RxLib()
        {
            BinRXD.EncryptionContainerName = "ReXLib";
        }

        static BinRXD RxdMain = BinRXD.Create();

        public static bool XmlToRxc(string xmlFileName, string rxcFileName)
        {
            try
            {
                using (BinRXD rxd = BinRXD.Load(xmlFileName))
                    return rxd.ToRXD(rxcFileName);
            }
            catch
            {
                return false;
            }
        }

        public static bool ConvertData(string inputpath, string outputpath, string customformat = null, string EncryptionKeyFile = null)
        {
            try
            {
                if (EncryptionKeyFile is not null && File.Exists(EncryptionKeyFile))
                    try
                    {
                        BinRXD.EncryptionKeysBlob = File.ReadAllBytes(EncryptionKeyFile);
                    }
                    catch { }

                if (customformat is null)
                    customformat = "";

                using (BinRXD rxd = BinRXD.Load(inputpath))
                    if (rxd is null)
                        return false;
                    else
                        return DataHelper.Convert(rxd, null, rxd.ToDoubleData(), null, outputpath, outputFormat: customformat).Result;
            }
            catch
            {
                return false;
            }
        }

        public static string LastConvertStatus() => DataHelper.LastConvertMessage;

        public static bool AddAccelerometer()
        {
            BinAccelerometer binAccelerometer = new BinAccelerometer();
            binAccelerometer.header.uniqueid = 5;
            RxdMain.Add(binAccelerometer);
            binAccelerometer[BinAccelerometer.BinProp.Axis] = 0;
            binAccelerometer[BinAccelerometer.BinProp.PhysicalNumber] = 0;
            binAccelerometer[BinAccelerometer.BinProp.RangeLow] = -4;
            binAccelerometer[BinAccelerometer.BinProp.RangeHi] = 4;
            binAccelerometer[BinAccelerometer.BinProp.SamplingRate] = 1000;
            return true;
        }

        public static bool RXCtoLiveDataUIDs(string inputpath, out byte buscount, out CanBusInfo[] uidlist)
        {
            buscount = 0;
            uidlist = null;

            BinRXD rxd = BinRXD.Load(inputpath);
            if (rxd is null)
                return false;

            var canlist = rxd.Where(x=>x.Value is BinCanInterface).Select(b => (BinCanInterface)b.Value).ToList();
            var msglist = rxd.Where(can=>can.Value is BinCanMessage).Select(b => (BinCanMessage)b.Value).ToList();
            msglist = msglist.Where(m => m[BinCanMessage.BinProp.InterfaceUID] > 0).ToList();
            var errlist = rxd.Where(err => err.Value is BinCanError).Select(b => (BinCanError)b.Value).ToList();
            errlist = errlist.Where(e => e[BinCanError.BinProp.InterfaceID] > 0).ToList();

            buscount = (byte)canlist.Count;
            uidlist = new CanBusInfo[buscount];

            for (int i = 0; i < buscount; i++)
            {
                uidlist[i].channel = canlist[i][BinCanInterface.BinProp.PhysicalNumber];

                var msg = msglist.FirstOrDefault(m => m[BinCanMessage.BinProp.InterfaceUID] == canlist[i].header.uniqueid);
                if (msg is null)
                    uidlist[i].CanFrame = 0;
                else
                    uidlist[i].CanFrame = msg.header.uniqueid;

                var err = errlist.FirstOrDefault(m => m[BinCanError.BinProp.InterfaceID] == canlist[i].header.uniqueid);
                if (err is null)
                    uidlist[i].ErrorFrame = 0;
                else
                    uidlist[i].ErrorFrame = err.header.uniqueid;
            }

            return true;
        }
    }
}
