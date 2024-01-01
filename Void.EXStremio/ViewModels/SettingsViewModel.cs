using System;
using System.Windows.Input;
using Microsoft.Win32;
using Void.EXStremio.Models;
using Void.EXStremio.WPF;

namespace Void.EXStremio.ViewModels {
    internal class SettingsViewModel : ViewModelBase, ICanRequestClose {
        readonly AppRoot appRoot;

        public event Action<bool?> CloseRequest;

        string executablePath;
        public string ExecutablePath {
            get { return executablePath; }
            set {
                executablePath = value;
                Notify();
            }
        }

        bool startStremio;
        public bool StartStremio {
            get { return startStremio; }
            set {
                startStremio = value;
                Notify();
            }
        }

        bool closeStremio;
        public bool CloseStremio {
            get { return closeStremio; }
            set {
                closeStremio = value;
                Notify();
            }
        }

        bool closeWithStremio;
        public bool CloseWithStremio {
            get { return closeWithStremio; }
            set {
                closeWithStremio = value;
                Notify();
            }
        }

        public ICommand SelectFileCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveCommand { get; }

        public SettingsViewModel(AppRoot appRoot) {
            this.appRoot = appRoot;
            SelectFileCommand = new RelayCommand<object>(OnSelectFile);
            CancelCommand = new RelayCommand<object>(OnCancel);
            SaveCommand = new RelayCommand<object>(OnSave);

            ReadConfig();
        }

        void ReadConfig() {
            ExecutablePath = appRoot.Config.StremioExePath;
            StartStremio = appRoot.Config.StartStremio;
            CloseStremio = appRoot.Config.CloseStremio;
            CloseWithStremio = appRoot.Config.CloseWithStremio;
        }

        void OnSelectFile(object arg) {
            var view = new OpenFileDialog();
            view.Filter = "Stremio executable (stremio.exe)|stremio.exe";
            if (view.ShowDialog() != true) { return; }

            ExecutablePath = view.FileName;
        }

        void OnCancel(object arg) {
            CloseRequest?.Invoke(false);
        }

        void OnSave(object arg) {
            var config = new Config();
            config.StremioExePath = ExecutablePath;
            config.StartStremio = StartStremio;
            config.CloseStremio = CloseStremio;
            config.CloseWithStremio= CloseWithStremio;
            Config.Save(config);
            appRoot.ReloadConfig();

            CloseRequest?.Invoke(true);
        }
    }
}
