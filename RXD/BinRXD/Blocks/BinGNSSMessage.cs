using InfluxShared.FileObjects;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RXD.Blocks
{
    #region Enumerations for Property type definitions
    public enum TypeGNSS : byte
    {
        LATITUDE,
        LONGITUDE,
        ALTITUDE,
        DATETIME,
        SPEED_OVER_GROUND,
        GROUND_DISTANCE,
        COURSE_OVER_GROUND,
        GEOID_SEPARATION,
        NUMBER_SATELLITES,
        QUALITY,
        HORIZONTAL_ACCURACY,
        VERTICAL_ACCURACY,
        SPEED_ACCURACY
    }
    #endregion

    public class BinGNSSMessage : BinBase
    {
        internal enum BinProp
        {
            InterfaceUID,
            Type,
        }

        #region Do not touch these
        public BinGNSSMessage(BinHeader hs = null) : base(hs) { }

        internal dynamic this[BinProp index]
        {
            get => data.GetProperty(index.ToString());
            set => data.SetProperty(index.ToString(), value);
        }
        #endregion

        internal static Dictionary<TypeGNSS, string> GnssName = new()
        {
            { TypeGNSS.LATITUDE, "Latitude" },
            { TypeGNSS.LONGITUDE, "Longitude" },
            { TypeGNSS.ALTITUDE, "Altitude" },
            { TypeGNSS.DATETIME, "Date/Time" },
            { TypeGNSS.SPEED_OVER_GROUND, "Speed over ground" },
            { TypeGNSS.GROUND_DISTANCE, "Ground distance" },
            { TypeGNSS.COURSE_OVER_GROUND, "Course over ground" },
            { TypeGNSS.GEOID_SEPARATION, "Geoid separation" },
            { TypeGNSS.NUMBER_SATELLITES, "Number of satellites" },
            { TypeGNSS.QUALITY, "Quality" },
            { TypeGNSS.HORIZONTAL_ACCURACY, "Horizontal accuracy" },
            { TypeGNSS.VERTICAL_ACCURACY, "Vertical accuracy" },
            { TypeGNSS.SPEED_ACCURACY, "Speed accuracy" },
        };

        internal static Dictionary<TypeGNSS, Type> GnssType = new()
        {
            { TypeGNSS.LATITUDE, typeof(Double) },
            { TypeGNSS.LONGITUDE, typeof(Double) },
            { TypeGNSS.ALTITUDE, typeof(Single) },
            { TypeGNSS.DATETIME, typeof(UInt32) },
            { TypeGNSS.SPEED_OVER_GROUND, typeof(Single) },
            { TypeGNSS.GROUND_DISTANCE, typeof(Single) },
            { TypeGNSS.COURSE_OVER_GROUND, typeof(Single) },
            { TypeGNSS.GEOID_SEPARATION, typeof(Single) },
            { TypeGNSS.NUMBER_SATELLITES, typeof(Single) },
            { TypeGNSS.QUALITY, typeof(Single) },
            { TypeGNSS.HORIZONTAL_ACCURACY, typeof(Single) },
            { TypeGNSS.VERTICAL_ACCURACY, typeof(Single) },
            { TypeGNSS.SPEED_ACCURACY, typeof(Single) },
        };

        internal override string GetName => this[BinProp.Type].ToString();
        //internal override string GetUnits => "";
        internal override ChannelDescriptor GetDataDescriptor => new()
        {
            StartBit = 0,
            BitCount = (ushort)(8 * Marshal.SizeOf(GnssType[this[BinGNSSMessage.BinProp.Type]] as Type)),
            isIntel = true,
            HexType = GnssType[this[BinGNSSMessage.BinProp.Type]],
            conversionType = ConversionType.None,
            Name = GetName,
            Units = GetUnits
        };

        internal override void SetupVersions()
        {
            Versions[1] = new Action(() =>
            {
                data.AddProperty(BinProp.InterfaceUID, typeof(UInt16));
                data.AddProperty(BinProp.Type, typeof(TypeGNSS));

                AddInput(BinProp.InterfaceUID.ToString());
            });
        }
    }
}
