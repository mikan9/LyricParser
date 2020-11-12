using System.Windows;


namespace LyricParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindowView : Window
    {
        public MainWindowView()
        {
            InitializeComponent();

            Top = Properties.Settings.Default.Top >= 0 ? Properties.Settings.Default.Top : 0;
            Left = Properties.Settings.Default.Left >= 0 ? Properties.Settings.Default.Left : 0;
            Height = Properties.Settings.Default.Height;
            Width = Properties.Settings.Default.Width;
            if (Properties.Settings.Default.Maximized) WindowState = WindowState.Maximized;
        }
    }
}
