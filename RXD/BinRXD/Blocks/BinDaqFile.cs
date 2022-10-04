using System;
using System.Collections.Generic;
using System.Text;

namespace RXD.Blocks
{
    internal class BinDaqFile : BinBase
    {
        public enum FileType : byte
        {
            JSON,
            XML,
        }

        internal enum BinProp
        {
            NameSize,
            Name,
            UploadRate,
            InputUID,
            InputTypeID,
            FileType
        }

        #region Do not touch these
        public BinDaqFile(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.NameSize, typeof(byte));
                data.AddProperty(BinProp.Name, typeof(string), BinProp.NameSize);
                data.AddProperty(BinProp.UploadRate, typeof(UInt32));
                data.AddProperty(BinProp.InputUID, typeof(UInt16));
                data.AddProperty(BinProp.InputTypeID, typeof(UInt16));
                data.AddProperty(BinProp.FileType, typeof(FileType));
            });
        }
    }
}
