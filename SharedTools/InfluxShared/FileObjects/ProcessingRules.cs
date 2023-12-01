using System;
using System.Collections.Generic;
using System.Text;

namespace InfluxShared.FileObjects
{
    public enum SamplingValueSource { Unknown, LastValue, NearestValue, Interpolation }
    public enum SyncTimestampLogic { Unsynced, Zero, FirstSample }

    public class ProcessingRules
    {
        private ProcessingRulesCollection Collection = null;

        public UInt64 SamplingRate { get; set; }
        public SamplingValueSource SamplingMethod { get; set; }
        public SyncTimestampLogic InitialTimestamp { get; set; }
        public bool SampleBeforeBeginning { get; set; }
        public bool SampleAfterEnd { get; set; }

        public ProcessingRules(ProcessingRulesCollection Owner)
        {
            Collection = Owner;
        }

        internal static ProcessingRules CopyFrom(ProcessingRulesCollection Owner, ProcessingRules rules)
        {
            if (rules is null)
                return new ProcessingRules(Owner);

            return new ProcessingRules(Owner)
            {
                SamplingRate = rules.SamplingRate,
                SamplingMethod = rules.SamplingMethod,
                InitialTimestamp = rules.InitialTimestamp,
                SampleBeforeBeginning = rules.SampleBeforeBeginning,
                SampleAfterEnd = rules.SampleAfterEnd,
            };
        }

        internal double InitialTime => InitialTimestamp switch
        {
            SyncTimestampLogic.Unsynced => double.NaN,
            SyncTimestampLogic.Zero => 0,
            SyncTimestampLogic.FirstSample => Collection.FirstTime,
            _ => throw new NotImplementedException(),
        };
    }
}
