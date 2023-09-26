using DbcParserLib.Model;

namespace DbcParserLib
{
    public interface IDbcBuilder
    {
        void AddMessage(Message message);
        void AddMessageComment(uint messageID, string comment);
        void AddNamedValueTable(string name, string values);
        void AddNode(Node node);
 //       void AddNodeComment(string nodeName, string comment);
        void AddSignal(Signal signal);
        void AddSignalComment(uint messageID, string signalName, string comment);
        void LinkNamedTableToSignal(uint messageId, string signalName, string tableName);
        void LinkTableValuesToSignal(uint messageId, string signalName, string values);

        //        Influx Technology LTD
        void AddDBCAttribute(DBCAttribute attribute);
        void AddEnvVariable(EnvVariable variable);
        DBCAttribute GetDBCAttribute(string name);
        void AddAttribute(string name, string value);
        Node GetNode(string name);
        Message GetMessage(uint ID);
        Signal GetSignal(uint ID, string name);
        EnvVariable GetEnvVariable(string name);
    }
}