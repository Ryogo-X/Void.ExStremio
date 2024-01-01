using Void.EXStremio.WPF;

namespace Void.EXStremio {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : WindowBase {
        public MainView() {
            InitializeComponent();
        }

        void OnLoaded(object sender, System.Windows.RoutedEventArgs e) {
            Visibility = System.Windows.Visibility.Hidden;
        }
    }
}