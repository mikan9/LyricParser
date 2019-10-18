using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LyricParser
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Properties.UserSettings.Default.Reload();

            LoadTheme();
        }

        public void LoadTheme()
        {
            Uri resUri = new Uri("/Resources.xaml", UriKind.Relative);
            Uri themeUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Themes\" + Properties.UserSettings.Default.ThemePath);

            var resource = Application.Current.Windows[1].Resources.MergedDictionaries;
            resource.Clear();

            var resDic = new ResourceDictionary();
            var themeDic = new ResourceDictionary();

            resDic.Source = resUri;
            themeDic.Source = themeUri;

            resDic.MergedDictionaries.Add(themeDic);
            resource.Add(resDic);
        }

        public bool newSettings = false;

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Save();
            Close();
        }

        private void Save()
        {
            newSettings = true;
            bool debugMode = (bool)debugFlag.IsChecked;
            int debugCategory = DebugCategories.SelectedIndex;
            int maxRetries = Properties.UserSettings.Default.MaxRetries;
            int.TryParse(MaxRetriesTxt.Text, out maxRetries);
            Properties.UserSettings.Default.ThemePath = Properties.UserSettings.Default.Themes[themeBox.SelectedIndex] + ".xaml";
            Properties.UserSettings.Default.MaxRetries = maxRetries;
            Properties.UserSettings.Default.Save();
        }
    }
}
