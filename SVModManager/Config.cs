using System.Collections.ObjectModel;
using System.IO;

namespace SVModManager
{
    public static class Config
    {
        public static ObservableCollection<ModInfo> ModInfoList { get; set; } = new ObservableCollection<ModInfo>();
        public static DirectoryInfo ModFolder { get; set; } = new DirectoryInfo("mods");
        public static DirectoryInfo BackupFolder { get; set; } = new DirectoryInfo("backup");
    }
}