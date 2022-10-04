using RxLibrary;
using System;
using System.IO;

namespace dotnetproject
{
    class Program
    {


        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Input and output files are not specified!");
                return;
            }


            string inputpath = args[0];
            string outputpath = args[1];
            string encryption = (args.Length > 2) ? args[2] : "";
            bool result;

            if (Path.GetExtension(inputpath).ToLower() == ".xml")
                result = RxLib.XmlToRxc(inputpath, outputpath);
            else
                result = RxLib.ConvertData(inputpath, outputpath, EncryptionKeyFile: encryption);

            if (result)
                Console.WriteLine("File " + Path.GetFileName(inputpath) + " successfully exported to " + Path.GetFileName(outputpath));
            else
                Console.WriteLine(RxLib.LastConvertStatus());
        }
    }
}
