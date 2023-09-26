using System;
using System.Text.RegularExpressions;

namespace DbcParserLib.Parsers
{
    public class EnvVariableLineParser : ILineParser
    {
        private const string EnvVariableLineStarter = "EV_";
        private const string EnvVariableRegex = @"EV_\s+([\w]+)\s*(\d+)\s+([\d\+\-eE.]+)\s+([\d\+\-eE.]+)\s+""(.*)""\s+([\d\+\-eE.]+)\s+(\d+)\s+([\w]+)\s+([\w\s,]+)";

        public bool TryParse(string line, IDbcBuilder builder)
        {
            if (line.TrimStart().StartsWith(EnvVariableLineStarter) == false)
                return false;

            line = line.Replace(":", "").Replace("[", "").Replace("]", "").Replace("|", " ");//Replace(',', ' ').

            var match = Regex.Match(line, EnvVariableRegex);
            if (match.Success == false)
                return false;

            var envVar = new EnvVariable();

            envVar.Name = match.Groups[1].Value;
            // EnvVarType.Data not supported (ENVVAR_DATA_)
            envVar.EnvVarType = Convert.ToUInt32(match.Groups[2].Value) == 1 ? EnvVarType.Float : EnvVarType.Int;
            var pos = line.IndexOf("DUMMY_NODE_VECTOR8", 0);
            if (pos != -1)
                envVar.EnvVarType = EnvVarType.String;
            envVar.Minimum = Convert.ToDouble(match.Groups[3].Value);
            envVar.Maximum = Convert.ToDouble(match.Groups[4].Value);
            envVar.Units = match.Groups[5].Value;
            envVar.InitialValue = Convert.ToDouble(match.Groups[6].Value);
            //match.Groups[7].Value not implemented 
            var allowInt = Convert.ToUInt32(match.Groups[8].Value.Substring(match.Groups[8].Length - 1));
            envVar.AllowRead = allowInt == 1 || allowInt == 3;
            envVar.AllowWrite = allowInt == 2 || allowInt == 3;

            var records = match.Groups[9].Value.Replace(",", " ").SplitBySpace();
            for (var i = 0; i < records.Length; i++)
                envVar.Nodes.Add(records[i]);

            builder.AddEnvVariable(envVar);
            return true;
        }
    }
}