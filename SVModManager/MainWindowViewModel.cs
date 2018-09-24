using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SVModManager.Annotations;

namespace SVModManager
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private DirectoryInfo _backupFolder;
        private DirectoryInfo _modFolder;

        public ObservableCollection<ModInfo> ModInfoList { get; set; }

        public DirectoryInfo ModFolder
        {
            get => _modFolder;
            set
            {
                _modFolder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModFolderPath));
            }
        }

        public string ModFolderPath => _modFolder?.FullName ?? "";

        public DirectoryInfo BackupFolder
        {
            get => _backupFolder;
            set
            {
                _backupFolder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackupFolderPath));
            }
        }

        public string BackupFolderPath => _backupFolder?.FullName ?? "";

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}