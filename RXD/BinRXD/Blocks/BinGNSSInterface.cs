using System;

namespace RXD.Blocks
{
    public class BinGNSSInterface : BinBase
    {
        public enum GNSSPlatformType : byte
        {
            PORTABLE = 0,
            STATIONARY = 2,
            PEDESTRIAN = 3,
            AUTOMOTIVE = 4,
            SEA = 5,
            AIRBORNE_UP_TO_1g = 6,
            AIRBORNE_UP_TO_2g = 7,
            AIRBORNE_UP_TO_4g = 8,
            WRIST = 9,
            MOTORBIKE = 10,
            ROBOTIC_LAWN_MOWER = 11,
            ELECTRIC_KICK_SCOOTER = 12
        }

        public enum MountAlignment : byte
        {
            Manual,
            Auto
        }

        public enum GNSSSystemType : byte
        {
            GPS,
            SBAS,
            Galileo,
            BeiDou,
            IMES,
            QZSS,
            GLONASS,
        }

        internal enum BinProp
        {
            PhysicalNumber,
            SamplingRate,
            Platform,
            MinimumSat,
            FilterCourseOverGround,
            FilterVelocity,
            AutomaticMountAlignment,
            MountYaw,
            MountPitch,
            MountRoll,
            GNSS_SystemCount,
            Type,
            ChannelMin,
            ChannelMax,
            GeofenceCount,
            Latitude,
            Longitude,
            Radius,
            IMU_Enable,
        }

        #region Do not touch these
        public BinGNSSInterface(BinHeader hs = null) : base(hs) { }

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
                data.AddProperty(BinProp.PhysicalNumber, typeof(byte));
                data.AddProperty(BinProp.SamplingRate, typeof(UInt16));
                AddOutput("");
            });
            Versions[2] = new Action(() =>
            {
                Versions[1].DynamicInvoke();
                data.AddProperty(BinProp.Platform, typeof(GNSSPlatformType), DefaultValue: GNSSPlatformType.AUTOMOTIVE);
                data.AddProperty(BinProp.MinimumSat, typeof(byte));
                data.AddProperty(BinProp.FilterCourseOverGround, typeof(bool));
                data.AddProperty(BinProp.FilterVelocity, typeof(bool));
                data.AddProperty(BinProp.AutomaticMountAlignment, typeof(bool));
                data.AddProperty(BinProp.MountYaw, typeof(Single));
                data.AddProperty(BinProp.MountPitch, typeof(Single));
                data.AddProperty(BinProp.MountRoll, typeof(Single));
                data.AddProperty(BinProp.GNSS_SystemCount, typeof(byte));
                data.AddProperty(BinProp.Type, typeof(GNSSSystemType[]), BinProp.GNSS_SystemCount);
                data.AddProperty(BinProp.ChannelMin, typeof(byte[]), BinProp.GNSS_SystemCount);
                data.AddProperty(BinProp.ChannelMax, typeof(byte[]), BinProp.GNSS_SystemCount);
                data.Property(BinProp.Type).XmlSequenceGroup = "GNSS_System";
                data.Property(BinProp.ChannelMin).XmlSequenceGroup = "GNSS_System";
                data.Property(BinProp.ChannelMax).XmlSequenceGroup = "GNSS_System";
                data.AddProperty(BinProp.GeofenceCount, typeof(byte));
                data.AddProperty(BinProp.Latitude, typeof(Single[]), BinProp.GeofenceCount);
                data.AddProperty(BinProp.Longitude, typeof(Single[]), BinProp.GeofenceCount);
                data.AddProperty(BinProp.Radius, typeof(ushort[]), BinProp.GeofenceCount);
                data.Property(BinProp.Latitude).XmlSequenceGroup = "Geofence";
                data.Property(BinProp.Longitude).XmlSequenceGroup = "Geofence";
                data.Property(BinProp.Radius).XmlSequenceGroup = "Geofence";
            });
            Versions[3] = new Action(() =>
            {
                Versions[2].DynamicInvoke();
                data.AddProperty(BinProp.IMU_Enable, typeof(bool));
            });
        }
    }
}
