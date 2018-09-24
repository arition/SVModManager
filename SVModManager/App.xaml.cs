using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace SVModManager
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            InitLogging();
            InitEnv();
            InitConfig();
        }

        public void InitLogging()
        {
            var config = new LoggingConfiguration();

            var logfile = new FileTarget("logfile")
            {
                FileName = "log.txt",
                MaxArchiveFiles = 2,
                ArchiveAboveSize = 100 * 1024
            };
            var logconsole = new ConsoleTarget("logconsole");
            var logConflict = new MemoryTarget("logConflict") {Layout = "${message}"};

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConflict, "conflict");

            LogManager.Configuration = config;
        }

        public void InitEnv()
        {
            if (!Config.ModFolder.Exists) Config.ModFolder.Create();
            if (!Config.BackupFolder.Exists) Config.BackupFolder.Create();
        }

        public void InitConfig()
        {
            var configFile = new FileInfo("config.json");
            if (configFile.Exists)
                using (var reader = configFile.OpenText())
                {
                    var modInfoList = JsonConvert.DeserializeObject<List<ModInfo>>(reader.ReadToEnd());
                    Config.ModInfoList.Clear();
                    foreach (var modInfo in modInfoList) Config.ModInfoList.Add(modInfo);
                }

            var modFolder = new DirectoryInfo("mods");
            foreach (var subDir in modFolder.EnumerateDirectories())
                if (Config.ModInfoList.AsParallel().All(t => t.Name != subDir.Name))
                    Config.ModInfoList.Add(new ModInfo {Name = subDir.Name});

            var subDirs = modFolder.GetDirectories();
            foreach (var modInfo in Config.ModInfoList.ToList())
                if (subDirs.AsParallel().All(t => t.Name != modInfo.Name))
                    Config.ModInfoList.Remove(modInfo);

            Config.ModInfoList.CollectionChanged += (sender, e) =>
            {
                var json = JsonConvert.SerializeObject(Config.ModInfoList, Formatting.Indented);
                using (var writer = File.CreateText("config.json"))
                {
                    writer.Write(json);
                }
            };

            // Since nothing will be removed or added to the list afterwards, we can safely use the trick 
            foreach (var modInfo in Config.ModInfoList)
                modInfo.PropertyChanged += (sender, e) =>
                {
                    var json = JsonConvert.SerializeObject(Config.ModInfoList, Formatting.Indented);
                    using (var writer = File.CreateText("config.json"))
                    {
                        writer.Write(json);
                    }
                };
        }
    }
}