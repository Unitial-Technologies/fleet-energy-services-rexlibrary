using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace InfluxShared.FileObjects
{
    internal class DataTransformer
    {
        private class Sample
        {
            internal double time;
            internal double value;
            internal bool Written = false;

            internal Sample(double time, double value)
            {
                this.time = time;
                this.value = value;
            }
        }

        public Action<double, double> Writer;

        ProcessingRules rules;

        List<Sample> samples = new List<Sample>();
        bool FirstSample = true;
        double StartTime;

        double TimeToWrite = double.NaN;

        internal DataTransformer(ProcessingRules rules)
        {
            this.rules = rules;
            StartTime = rules.InitialTime;
        }

        public void Push(double time, double data)
        {
            if (FirstSample)
            {
                TimeToWrite = double.IsNaN(StartTime) ? time : StartTime;
                FirstSample = false;
                if (rules.SampleBeforeBeginning)
                {
                    while (TimeToWrite <= time)
                    {
                        Writer?.Invoke(TimeToWrite, data);
                        TimeToWrite += (double)rules.SamplingRate / 1000;
                    }
                }
                else
                {
                    while (TimeToWrite < time)
                        TimeToWrite += (double)rules.SamplingRate / 1000;
                }
            }

            samples.Add(new Sample(time, data));
            while (samples.Count > 1 && samples[1].time < TimeToWrite)
                samples.RemoveAt(0);

            if (samples.Count > 1)
            {
                while (TimeToWrite >= samples[0].time && TimeToWrite <= samples[1].time)
                {
                    var newdata = rules.SamplingMethod switch
                    {
                        SamplingValueSource.LastValue => samples[0].value,
                        SamplingValueSource.NearestValue => (TimeToWrite - samples[0].time < samples[1].time - TimeToWrite) ? samples[0].value : samples[1].value,
                        _ => throw new NotImplementedException(),
                    };
                    Writer?.Invoke(TimeToWrite, newdata);
                    TimeToWrite += (double)rules.SamplingRate / 1000;
                }
                samples.RemoveAt(0);
            }
        }

        internal void PushEnd(double time)
        {
            if (samples.Count == 0)
                return;

            Writer?.Invoke(TimeToWrite, samples[samples.Count - 1].value);
            TimeToWrite += (double)rules.SamplingRate / 1000;

            if (rules.SampleAfterEnd)
            {
                while (TimeToWrite <= time)
                {
                    Writer?.Invoke(TimeToWrite, samples[samples.Count - 1].value);
                    TimeToWrite += (double)rules.SamplingRate / 1000;
                }
                if (TimeToWrite <= time + (double)rules.SamplingRate / 1000)
                    Writer?.Invoke(TimeToWrite, samples[samples.Count - 1].value);
            }
        }

    }
}
