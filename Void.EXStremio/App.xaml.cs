using System.Windows;
using Void.EXStremio.Models;
using Void.EXStremio.Utility;
using Void.EXStremio.ViewModels;

namespace Void.EXStremio {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            var appRoot = new AppRoot();
            var vm = new MainViewModel(new AppRoot());
            var view = new MainView();
            view.DataContext = vm;

            view.ShowDialog();
        }
    }
}