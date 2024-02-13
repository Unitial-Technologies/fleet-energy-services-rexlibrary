namespace RXD.Objects
{
    internal interface IRecordGridItem
    {
        public string strTimestamp { get; }

        public string strBusChannel { get; }

        public string strCanID { get; }

        public string strFlags { get; }

        public string strDLC { get; }

        public string strData { get; }
    }
}
