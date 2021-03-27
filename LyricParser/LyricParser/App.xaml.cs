using LyricParser.Repository;
using LyricParser.ViewModels;
using Prism.Ioc;
using Prism.Unity;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace LyricParser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        static LyricsDatabase database;

        public App() :base()
        {
            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        public static LyricsDatabase Database
        {
            get
            {
                if(database == null)
                {
                    database = new LyricsDatabase();
                }
                return database;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Thread.CurrentThread.CurrentCulture = new CultureInfo(LyricParser.Properties.UserSettings.Default.Locale);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(LyricParser.Properties.UserSettings.Default.Locale);
        }
        public static void ChangeCulture(CultureInfo newCulture)
        {
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MainWindowView, MainWindowViewModel>();
            containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
            containerRegistry.RegisterForNavigation<EditorView, EditorViewModel>();

            containerRegistry.RegisterDialog<SettingsView>();
            containerRegistry.RegisterDialog<EditorView>();
        }

        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindowView>();
            return w;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled exception occurred: \n" + e.Exception.Message + "\n " + e.Exception.InnerException, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
