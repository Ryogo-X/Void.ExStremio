using System;
using System.Windows;
using System.Windows.Threading;

namespace Void.EXStremio.Models {
    internal class AppRoot {
        public event Action Exit;
        public event Action<IConfig> ConfigReloaded;
        public Window MainView {
            get { return Application.Current.MainWindow; }
        }
        public Dispatcher Dispatcher {
            get { return Application.Current.Dispatcher; }
        }
        public IConfig Config { get; private set; }

        public AppRoot() {
            Application.Current.Exit += OnApplicationExit;
            ReloadConfig();
        }

        void OnApplicationExit(object sender, ExitEventArgs e) {
            Exit?.Invoke();
        }

        public void ReloadConfig() {
            Config = Models.Config.Load() ?? new Config();
            ConfigReloaded?.Invoke(Config);
        }
    }
}
