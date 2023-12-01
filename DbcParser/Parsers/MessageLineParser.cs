using DbcParser.Parsers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DbcParserLib.Parsers
{
    public class MessageLineParser : ILineParser
    {
        private const string MessageLineStarter = "BO_ ";
        private const string MessageRegex = @"BO_ (\d+)\s+(\w+)\s*:\s*(\d+)\s+(\w+)";

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
                if (!AttributeDefaultParser.ID_List.Contains(id))
                    AttributeDefaultParser.ID_List.Add(id);
                msg.Type = CheckExtID(ref id);
                msg.ID = id;
                builder.AddMessage(msg);
            }

            return true;
        }

        private Message.MsgType CheckExtID(ref uint id)
        {
            // For extended ID bit 31 is always 1
            if (id >= 0x80000000)
            {
                //               id -= 0x80000000;
                return Message.MsgType.Extended;
            }
            else
                return Message.MsgType.Standard;
        }
    }
}