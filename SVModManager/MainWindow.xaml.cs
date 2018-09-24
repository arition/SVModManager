using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using NLog.Targets;

namespace SVModManager
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            Init();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private Logger Logger { get; } = LogManager.GetCurrentClassLogger();
        public MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        public void Init()
        {
            ViewModel.ModFolder = Config.ModFolder;
            ViewModel.BackupFolder = Config.BackupFolder;
            ViewModel.ModInfoList = Config.ModInfoList;
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var svLocation =
                Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\LocalLow\Cygames\Shadowverse");

            var perModPercent = 1d / ViewModel.ModInfoList.Count;
            double progressValue;

            var progress = await this.ShowProgressAsync("Processing", "Indexing files...");
            // ReSharper disable once CollectionNeverQueried.Local
            var conflictBin = new ConcurrentDictionary<string, string>();
            var conflictLog = LogManager.GetLogger("conflict");

            await Task.Run(() =>
            {
                Parallel.ForEach(ViewModel.ModInfoList, modInfo =>
                {
                    modInfo.FileList.Clear();
                    foreach (var file in Directory.EnumerateFiles(Path.Combine(ViewModel.ModFolderPath, modInfo.Name),
                        "*", SearchOption.AllDirectories))
                    {
                        var fileInfo = new FileInfo(file);
                        if (file.EndsWith(".unity3d") || file.EndsWith(".acb")) modInfo.FileList.Add(fileInfo);

                        if (!conflictBin.TryAdd(fileInfo.Name, modInfo.Name))
                            conflictLog.Log(LogLevel.Warn,
                                $"Find conflict for file {fileInfo.Name} in mod {modInfo.Name}");
                    }
                });
            });

            var target = LogManager.Configuration.FindTargetByName<MemoryTarget>("logConflict");
            if (target.Logs.Count != 0)
            {
                var result = await this.ShowMessageAsync("Find Conflicts", string.Join("\n", target.Logs),
                    MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings {AffirmativeButtonText = "Cancel", NegativeButtonText = "Continue"});
                if (result == MessageDialogResult.Affirmative || result == MessageDialogResult.Canceled)
                {
                    await progress.CloseAsync();
                    return;
                }
            }

            progressValue = 0.1;
            progress.SetProgress(progressValue);
            progress.SetMessage("Backup files...");

            await Task.Run(() =>
            {
                foreach (var modInfo in ViewModel.ModInfoList)
                {
                    foreach (var file in modInfo.FileList)
                    {
                        if (File.Exists(Path.Combine(Config.BackupFolder.FullName, file.Name)))
                            continue;
                        switch (file.Extension)
                        {
                            case ".unity3d":
                                File.Copy(Path.Combine(svLocation, "a", file.Name),
                                    Path.Combine(Config.BackupFolder.FullName, file.Name));
                                break;
                            case ".acb":
                                File.Copy(Path.Combine(svLocation, "v", file.Name),
                                    Path.Combine(Config.BackupFolder.FullName, file.Name));
                                break;
                            default:
                                throw new Exception("Unsupported extension");
                        }
                    }

                    progressValue += perModPercent * 0.3;
                    progress.SetProgress(progressValue);
                }
            });

            progressValue = 0.4;
            progress.SetProgress(progressValue);
            progress.SetMessage("Replace files...");

            await Task.Run(() =>
            {
                foreach (var modInfo in ViewModel.ModInfoList.Reverse())
                {
                    foreach (var file in modInfo.FileList)
                        switch (file.Extension)
                        {
                            case ".unity3d":
                                file.CopyTo(Path.Combine(svLocation, "a", file.Name), true);
                                break;
                            case ".acb":
                                file.CopyTo(Path.Combine(svLocation, "v", file.Name), true);
                                break;
                            default:
                                throw new Exception("Unsupported extension");
                        }

                    progressValue += perModPercent * 0.3;
                    progress.SetProgress(progressValue);
                }
            });

            progressValue = 0.7;
            progress.SetProgress(progressValue);
            progress.SetMessage("Restore unmodded files...");

            await Task.Run(() =>
            {
                Task.Run(async () =>
                {
                    while (progressValue < 0.99)
                    {
                        progress.SetProgress(progressValue);
                        await Task.Delay(200);
                    }
                });
                var backupFileList = ViewModel.BackupFolder.GetFiles();
                var perFilePercent = 1d / backupFileList.Length;
                foreach (var file in backupFileList)
                {
                    if (conflictBin.Keys.All(t => t != file.Name))
                    {
                        switch (file.Extension)
                        {
                            case ".unity3d":
                                file.CopyTo(Path.Combine(svLocation, "a", file.Name), true);
                                break;
                            case ".acb":
                                file.CopyTo(Path.Combine(svLocation, "v", file.Name), true);
                                break;
                            default:
                                throw new Exception("Unsupported extension");
                        }

                        file.Delete();
                    }

                    progressValue += perFilePercent * 0.3;
                }
            });

            progressValue = 1;
            progress.SetProgress(progressValue);
            await progress.CloseAsync();
        }
    }
}