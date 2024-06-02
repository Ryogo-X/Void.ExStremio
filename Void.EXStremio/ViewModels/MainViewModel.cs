using System;
using System.IO;
using System.Windows.Input;
using Void.EXStremio.Models;
using Void.EXStremio.Utility;
using Void.EXStremio.Views;
using Void.EXStremio.Web;
using Void.EXStremio.WPF;

namespace Void.EXStremio.ViewModels {
    internal class MainViewModel : ICanRequestClose {
        readonly AppRoot appRoot;
        ProcessWatcher watcher;

        public event Action<bool?> CloseRequest;
        
        public ICommand SettingsCommand { get; }
        public ICommand CloseCommand { get; }

        public MainViewModel(AppRoot appRoot) {
            this.appRoot = appRoot;
            SettingsCommand = new RelayCommand<object>(OnSettings);
            CloseCommand = new RelayCommand<object>(OnClose);

            OnConfigReloaded(appRoot.Config);
            appRoot.ConfigReloaded += OnConfigReloaded;

            if (appRoot.Config.StartStremio) {
                watcher?.RunProcess();
            }

            var server = new WebServer();
            server.RunAsync();
        }

        void InitializeWatcher(IConfig config) {
            watcher?.StopWatching();
            watcher = null;

            if (string.IsNullOrWhiteSpace(config.StremioExePath)) { return; }
            if (!File.Exists(appRoot.Config.StremioExePath)) { return; }

            watcher = new ProcessWatcher(config.StremioExePath);
            watcher.StartWatching();
        }

        void OnConfigReloaded(IConfig config) {
            InitializeWatcher(config);

            appRoot.Exit -= OnAppExit;
            if (config.CloseStremio) {
                appRoot.Exit += OnAppExit;
            }

            if (watcher == null) { return; }

            watcher.ProcessExited -= OnStremioProcessExited;
            if (config.CloseWithStremio) {
                watcher.ProcessExited += OnStremioProcessExited;
            }

            config.InitializeEnvironmentVariables();
        }

        void OnStremioProcessExited() {
            appRoot.Dispatcher.Invoke(() => {
                appRoot.MainView.Close();
            });
        }

        void OnAppExit() {
            watcher.ProcessExited -= OnStremioProcessExited;
            watcher.StopProcess();
        }

        void OnSettings(object arg) {
            var vm = new SettingsViewModel(appRoot);
            var view = new SettingsView();
            view.Owner = appRoot.MainView;
            view.DataContext = vm;

            view.ShowDialog();
        }

        void OnClose(object arg) {
            CloseRequest?.Invoke(null);
        }
    }
}
