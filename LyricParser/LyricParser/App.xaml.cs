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
    public partial class App : Application
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
            Application.Current.MainWindow = new MainWindow();
            Application.Current.MainWindow.Show();

            oldWindow.Close();
        }
    }
}
