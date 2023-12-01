using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace InfluxShared.Helpers
{
    public interface FDialog
    {
        public string Filter { get; set; }
        public int FilterIndex { get; set; }
    }

    public static class StringHelper
    {
        private static byte CaseLookupSize = 0xFF;
        private static char[] LowerLookup = new char[CaseLookupSize];
        private static char[] UpperLookup = new char[CaseLookupSize];

        private static string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        static StringHelper()
        {
            for (int i = 0; i < CaseLookupSize; i++)
            {
                LowerLookup[i] = char.ToLowerInvariant((char)i);
                UpperLookup[i] = char.ToUpperInvariant((char)i);
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToLowerFastASCII(this string str)
        {
            char[] tmp = new char[str.Length];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = LowerLookup[str[i]];

            return new string(tmp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToUpperFastASCII(this string str)
        {
            char[] tmp = new char[str.Length];
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = UpperLookup[str[i]];

            return new string(tmp);
        }

        /*public static bool ValidEnChars(this string str)
        {
            for (int i = 0; i < str.Length; i++)
                if (!chars.Contains(str[i]))
                    return false;
            return true;
        }*/

        public static string ToTitleCase(this string str)
        {
            switch (str)
            {
                case null: return "";
                case "": return "";
                default: return str.First().ToString().ToUpper() + str.Substring(1).ToLower();
            }
        }

        public static string Repeat(this char chatToRepeat, int repeat)
        {
            return new string(chatToRepeat, repeat);
        }

        public static string Repeat(this string stringToRepeat, int repeat)
        {
            var builder = new StringBuilder(repeat * stringToRepeat.Length);
            for (int i = 0; i < repeat; i++)
            {
                builder.Append(stringToRepeat);
            }

            return builder.ToString();
        }

        public static string ReplaceInvalid(this string str, char[] AllowedChars, string ReplaceWith)
        {
            string tmp = "";
            foreach (char c in str)
                tmp += AllowedChars.Contains(c) ? c : ReplaceWith;

            return tmp;
        }

        public static string GenerateFileName(this string Directory, string Extension, int FileNameLength = 20)
        {
            Random random = new Random();

            string FileName;
            do
                FileName = Path.Combine(Directory, new string(Enumerable.Repeat(chars, FileNameLength).Select(s => s[random.Next(s.Length)]).ToArray()) + "." + Extension);
            while (File.Exists(FileName));

            return FileName;
        }

        public static uint ConvertFromHex(this string hex)
        {
            try
            {
                return Convert.ToUInt32(hex, 16);
            }
            catch (Exception)
            {
                return 0;
            }
        }

    }
}
