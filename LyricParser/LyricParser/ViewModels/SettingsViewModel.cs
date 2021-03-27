using Prism.Commands;
using System;
using System.Drawing.Text;
using System.Windows;

namespace LyricParser.ViewModels
{
    public class SettingsViewModel : DialogBase
    {
        public bool newSettings = false;

        private InstalledFontCollection installedFontCollection;

        private string _selectedFontFamily = "Segoe UI";
        private string _selectedTheme = "Dark";

        private int _maxRetries = 5;
        private int _selectedDebugCategory = 0;

        private bool _debugChecked = false;

        private string[] _fontFamilies = { };

        #region Properties

        // String properties
        public string SelectedFontFamily
        {
            get => _selectedFontFamily;
            set => SetProperty(ref _selectedFontFamily, value);
        }
        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        // String array properties
        public string[] FontFamilies
        {
            get => _fontFamilies;
            set => SetProperty(ref _fontFamilies, value);
        }

        // Int properties
        public int MaxRetries
        {
            get => _maxRetries;
            set => SetProperty(ref _maxRetries, value);
        }
        public int SelectedDebugCategory
        {
            get => _selectedDebugCategory;
            set => SetProperty(ref _selectedDebugCategory, value);
        }

        // Bool properties
        public bool DebugChecked
        {
            get => _debugChecked;
            set => SetProperty(ref _debugChecked, value);
        }

        #endregion

        // Commands
        public DelegateCommand SaveCommand { get; }

        public SettingsViewModel()
        {
            _title = "Settings";
            SaveCommand = new DelegateCommand(Save);

            Properties.UserSettings.Default.Reload();
            SelectedFontFamily = Properties.UserSettings.Default.FontFamily;
            LoadTheme();
            LoadFontFamilies();
        }

        public void LoadTheme()
        {
            Uri resUri = new Uri("/Resources/Resources.xaml", UriKind.Relative);
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
        
        public void LoadFontFamilies()
        {
            installedFontCollection = new InstalledFontCollection();

            int count = installedFontCollection.Families.Length;

            FontFamilies = new string[count];

            for (int i = 0; i < count; ++i)
            {
                FontFamilies[i] = installedFontCollection.Families[i].Name;
            }
        }

        private void Save()
        {
            newSettings = true;
            bool debugMode = DebugChecked;
            int debugCategory = SelectedDebugCategory;
            MaxRetries = Properties.UserSettings.Default.MaxRetries;
            Properties.UserSettings.Default.ThemePath = SelectedTheme + ".xaml";
            Properties.UserSettings.Default.MaxRetries = MaxRetries;
            Properties.UserSettings.Default.FontFamily = SelectedFontFamily;
            Properties.UserSettings.Default.Save();

            CloseDialog(newSettings.ToString());
        }
    }
}
