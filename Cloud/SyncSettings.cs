namespace Cloud
{
    internal class SyncSettings
    {
        internal bool enabled { get; set; } = false;
        internal string main_logger { get; set; } = "";
        internal string addon_loger1 { get; set; } = "";
        internal string addon_loger2 { get; set; } = "";
        internal string lastfolder { get; set; } = "";
    }
}