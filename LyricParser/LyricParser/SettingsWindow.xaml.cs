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
        }

        public bool newSettings = false;

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            Save();
            this.Close();
        }

        private void Save()
        {
            newSettings = true;
            bool debugMode = (bool)debugFlag.IsChecked;
            int debugCategory = debugCategories.SelectedIndex;
            int maxRetries = Properties.UserSettings.Default.MaxRetries;
            int.TryParse(maxRetriesTxt.Text, out maxRetries);
            Properties.UserSettings.Default.ThemePath = Properties.UserSettings.Default.Themes[themeBox.SelectedIndex] + ".xaml";
            Properties.UserSettings.Default.MaxRetries = maxRetries;
            Properties.UserSettings.Default.Save();
        }
    }
}
