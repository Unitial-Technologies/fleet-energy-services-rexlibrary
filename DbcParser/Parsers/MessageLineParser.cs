using DbcParser.Parsers;
using InfluxShared.FileObjects;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DbcParserLib.Parsers
{
    public class MessageLineParser : ILineParser
    {
        static readonly UInt32 ExtendedFlag = 0x80000000;
        private const string MessageLineStarter = "BO_ ";
        private const string MessageRegex = @"BO_ (\d+)\s+([A-Za-z0-9()_]+)\s*:\s*(\d+)\s+(\w+)";

        public bool TryParse(string line, IDbcBuilder builder)
        {
            if (line.Trim().StartsWith(MessageLineStarter) == false)
                return false;

            var match = Regex.Match(line, MessageRegex);
            if (match.Success)
            {
                var msg = new Message()
                {
                    Name = match.Groups[2].Value,
                    DLC = byte.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                    Transmitter = match.Groups[4].Value,
                };
                msg.ID = uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                uint id = msg.ID;
                if (!builder.AttrDefaultParser.ID_List.Contains(id))
                    builder.AttrDefaultParser.ID_List.Add(id);
                msg.Type = (id & ExtendedFlag) == ExtendedFlag ? DBCMessageType.Extended : DBCMessageType.Standard;
                msg.ID = id;
                builder.AddMessage(msg);
            }

            return true;
        }

    }
}