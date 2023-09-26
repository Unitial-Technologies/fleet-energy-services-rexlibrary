using DbcParser.Parsers;
using System;

namespace DbcParserLib.Parsers
{
    public class AttributeLineParser : ILineParser
    {
        private const string AttributeLineStarter = "BA_";

        public bool TryParse(string line, IDbcBuilder builder)
        {
            try
            {
                if (line.TrimStart().StartsWith(AttributeLineStarter) == false)
                    return false;

                line = line.Replace(',', ' ');
                var records = line.Trim(' ', ';').SplitBySpace();

                if (records.Length == 1)
                    return false;

                var cleanLine = line.Trim(' ', ';');

                if (cleanLine.StartsWith("BA_DEF_ "))
                {
                    if (records[1] == "BO_")
                        AddAttribute(ApplyTo.Message, records, builder);
                    else if (records[1] == "BU_")
                        AddAttribute(ApplyTo.Node, records, builder);
                    else if (records[1] == "SG_")
                        AddAttribute(ApplyTo.Signal, records, builder);
                    else if (records[1] == "EV_")
                        AddAttribute(ApplyTo.EnvVar, records, builder);
                    else
                        AddAttribute(ApplyTo.Project, records, builder);

                    return true;
                }

                if (cleanLine.StartsWith("BA_DEF_DEF_ "))
                {

                    AttributeDefaultParser.AttrTypes.Add(records[1].Replace("\"", ""), records[2].Replace("\"", ""));
                    AddAttributeDefault(records, builder);
                    return true;
                }

                if (cleanLine.StartsWith("BA_ "))
                {
                    if (records[2] == "BU_")
                        AddAttributeValue(ApplyTo.Node, records, builder);
                    else if (records[2] == "BO_")
                        AddAttributeValue(ApplyTo.Message, records, builder);
                    else if (records[2] == "SG_")
                        AddAttributeValue(ApplyTo.Signal, records, builder);
                    else if (records[2] == "EV_")
                        AddAttributeValue(ApplyTo.EnvVar, records, builder);
                    else if (records[2] != "")
                        AddAttributeValue(ApplyTo.Project, records, builder);

                    return true;
                }
            }
            catch (Exception)
            {

            }
            

            return false;
        }
        private static void AddAttribute(ApplyTo applyTo, string[] records, IDbcBuilder builder)
        {
            try
            {
                var idx = applyTo == ApplyTo.Project ? 1 : 2;
                var attr = new DBCAttribute();

                attr.Name = records[idx].Replace("\"", "");
                attr.ApplyTo = applyTo;
                Enum.TryParse<DataType>(records[idx + 1], true, out attr.DataType);

                if (attr.DataType == DataType.String)
                    attr.Default = "";
                if (attr.DataType == DataType.Int || attr.DataType == DataType.Hex)
                {
                    attr.Min = records[idx + 2];
                    attr.Max = records[idx + 3];
                }
                if (attr.DataType == DataType.Enum)
                    for (int i = idx + 2; i < records.Length; i++)
                        attr.Enums.Add(records[i].Replace("\"", ""));

                builder.AddDBCAttribute(attr);
            }
            catch (Exception)
            {

            }
            
        }

        private static void AddAttributeDefault(string[] records, IDbcBuilder builder)
        {
            var name = records[1].Replace("\"", "");
            var attr = builder.GetDBCAttribute(name);
            if (attr != null)
                attr.Default = records[2].Replace("\"", "");
        }

        private static void AddAttributeValue(ApplyTo applyTo, string[] records, IDbcBuilder builder)
        {
            var name = records[1].Replace("\"", "");

            if (applyTo == ApplyTo.Project)
            {
                var value = records[2].Replace("\"", "");

                builder.AddAttribute(name, value);
            }
            else if (applyTo == ApplyTo.Node)
            {
                var nodeName = records[3].Replace("\"", "");
                var node = builder.GetNode(nodeName);
                if (node == null)
                    return;

                var value = records[4].Replace("\"", "");
                node.AttrValues.Add(name, value);
            }
            else if (applyTo == ApplyTo.Message || applyTo == ApplyTo.Signal)
            {
                var ID = Convert.ToUInt32(records[3]);
                var msg = builder.GetMessage(ID);
                if (msg == null)
                    return;

                if (applyTo == ApplyTo.Message)
                    msg.AttrValues.Add(name, records[4]);
                else
                {
                    var sigName = records[4].Replace("\"", "");
                    var sig = builder.GetSignal(ID, sigName);
                    if (sig == null)
                        return;

                    var value = records[5].Replace("\"", "");
                    sig.AttrValues.Add(name, value);
                }
            }
            else if (applyTo == ApplyTo.EnvVar)
            {
                var envName = records[3].Replace("\"", "");
                var env = builder.GetEnvVariable(envName);
                if (env == null)
                    return;

                var value = records[4].Replace("\"", "");
                for (var i = 5; i < records.Length; i++)
                    value = value + " " + records[i].Replace("\"", "");

                env.AttrValues.Add(name, value);
            }
        }
    }
}