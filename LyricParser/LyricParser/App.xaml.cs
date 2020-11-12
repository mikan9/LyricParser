using LyricParser.ViewModels;
using Prism.Ioc;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LyricParser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        public App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Thread.CurrentThread.CurrentCulture = new CultureInfo(LyricParser.Properties.UserSettings.Default.Locale);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(LyricParser.Properties.UserSettings.Default.Locale);
            // LocaleResources.Culture = new CultureInfo("ja-JP");
        }
        public static void ChangeCulture(CultureInfo newCulture)
        {
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            var oldWindow = Application.Current.MainWindow;
            Application.Current.MainWindow = new MainWindowView();
            Application.Current.MainWindow.Show();

            //oldWindow.Close();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<MainWindowView, MainWindowViewModel>();
            containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
            containerRegistry.RegisterForNavigation<EditLyricsView, EditLyricsViewModel>();

            containerRegistry.RegisterDialog<SettingsView>();
            containerRegistry.RegisterDialog<EditLyricsView>();
        }

        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindowView>();
            return w;
        }
    }
}
