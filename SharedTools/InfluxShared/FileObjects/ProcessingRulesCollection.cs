using System;
using System.Collections.Generic;
using System.Text;

namespace InfluxShared.FileObjects
{
    public class ProcessingRulesCollection : Dictionary<object, ProcessingRules>, IDisposable
    {
        private bool disposedValue = false;

        internal ProcessingRules GeneralRules = null;
        internal double FirstTime = double.NaN;

        internal bool IndividualProcessing => GeneralRules is null;

        public ProcessingRulesCollection()
        {
        }

        #region Destructors
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ProcessingRulesCollection()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ProcessingRules GetProcessingRules(object item) => GeneralRules is null ? this[item] : GeneralRules;

        internal void Add(object item)
        {
            base.Add(item, ProcessingRules.CopyFrom(this, GeneralRules));

        }

    }
}
