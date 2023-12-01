using DbcParserLib.Parsers;
using System.Collections.Generic;
using System.IO;


namespace DbcParserLib
{
    public class Parser
    {


        private IEnumerable<ILineParser> LineParsers = new List<ILineParser>()
        {
            new IgnoreLineParser(), // Used to skip line we know we want to skip
            new NodeLineParser(),
            new MessageLineParser(),
            new CommentLineParser(),
            new SignalLineParser(),
            new ValueTableLineParser(),
            
            // Influx Technology LTD
            //place here
            new AttributeLineParser(),
            new EnvVariableLineParser(),
            new UnknownLineParser() // Used as a catch all 
            

        };

        public Dbc ParseFromPath(string dbcPath)
        {
            using (var fileStream = new FileStream(dbcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return ParseFromStream(fileStream);
            }
        }

        public Dbc ParseFromStream(Stream dbcStream)
        {
            using (var reader = new StreamReader(dbcStream))
            {
                return ParseFromReader(reader);
            }
        }

        public Dbc Parse(string dbcText)
        {
            using (var reader = new StringReader(dbcText))
            {
                return ParseFromReader(reader);
            }

        }

        private Dbc ParseFromReader(TextReader reader)
        {
            var builder = new DbcBuilder();

            while (reader.Peek() >= 0)
                ParseLine(reader.ReadLine(), builder);
            builder.SetDefaultAttrValues();

            return builder.Build();
        }

        private void ParseLine(string line, IDbcBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            foreach (var parser in LineParsers)
            {
                if (parser.TryParse(line, builder))
                    break;

            }

        }
    }
}