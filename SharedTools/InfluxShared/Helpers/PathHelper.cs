using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace InfluxShared.Helpers
{
    public static class PathHelper
    {
        public static string AppPath => Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        public static string AppDataRoamingPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Influx Technology", Assembly.GetExecutingAssembly().GetName().Name);
        public static string AppDataLocalPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Influx Technology", Assembly.GetExecutingAssembly().GetName().Name);
        public static string AppDataLocalRootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Influx Technology");
        public static string IniFilesPath => Path.Combine(AppDataLocalPath, "Ini");
        public static string ProjectFilesPath => Path.Combine(AppDataLocalPath, "Project");
        public static string LogFilesPath => Path.Combine(AppDataLocalPath, "Log");
        public static string ScreensFilesPath => Path.Combine(AppDataRoamingPath, "Screens");
        public static string TempPath
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Temp");
                }
                string temp = Path.Combine(AppDataLocalPath, "Temp");
                Directory.CreateDirectory(temp);
                return temp;
            }
        }

        public static string SharedAppDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Influx Technology");
        public static string LicensesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Influx Technology", "Licenses");
        public static string GlobalSettingsFile => Path.Combine(SharedAppDataPath, "Settings", Assembly.GetExecutingAssembly().GetName().Name + ".xml");
        public static string ZipPath { get; set; }

        static PathHelper()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Directory.CreateDirectory(AppDataLocalPath);
                Directory.CreateDirectory(IniFilesPath);
                Directory.CreateDirectory(ProjectFilesPath);
                Directory.CreateDirectory(LogFilesPath);
                Directory.CreateDirectory(ScreensFilesPath);
                Directory.CreateDirectory(LicensesPath);
            }
        }

        public static bool hasWriteAccessToFile(string path)
        {
            // First trying to remove readonly flag
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                fileInfo.IsReadOnly = false;
                File.SetAttributes(path, fileInfo.Attributes);
            }
            catch
            { }

            // Now testing if file can be opened in readwrite access
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
