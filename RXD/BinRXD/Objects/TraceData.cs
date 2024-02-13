namespace RXD.Objects
{
    internal class TraceData : TraceRow, IRecordGridItem, IItemGridDetails
    {
        public string strItemName => SourceChannel;

        public string strTimestamp => string.Format("{0:0.000000}", FloatTimestamp);

        public string SourceChannel { get; set; }

        public string strBusChannel => SourceChannel;

        public string strCanID => "";

        public string strFlags => "";

        public string strDLC => "";

        public double Value { get; set; }

        public string strData => Value.ToString();
    }
}
