using System.ComponentModel;
using System.Management;

namespace Influx.Shared.Objects
{
    public delegate void RexChangedNotify(bool Plugged);

    public class WmiWatcher
    {
        static string DeviceID = @"USB\VID_16D0&PID_0F14\";

        public RexChangedNotify RexChangedEvent = null;

        public WmiWatcher()
        {
            BackgroundWorker bgwDriveDetector = new BackgroundWorker();
            bgwDriveDetector.DoWork += bgwDriveDetector_DoWork;
            bgwDriveDetector.RunWorkerAsync();
            bgwDriveDetector.WorkerReportsProgress = true;
            bgwDriveDetector.WorkerSupportsCancellation = true;

        }

        public WmiWatcher(string deviceID) : this()
        {
            DeviceID = deviceID;
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            foreach (var property in instance.Properties)
                if (property.Name == "DeviceID" && string.Compare(DeviceID, 0, property.Value as string, 0, DeviceID.Length) == 0)
                {
                    RexChangedEvent?.Invoke(true);
                    return;
                }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            foreach (var property in instance.Properties)
                if (property.Name == "DeviceID" && string.Compare(DeviceID, 0, property.Value as string, 0, DeviceID.Length) == 0)
                {
                    RexChangedEvent?.Invoke(false);
                    return;
                }
        }

        private void bgwDriveDetector_DoWork(object sender, DoWorkEventArgs e)
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");

            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            removeWatcher.Start();
        }

    }
}
