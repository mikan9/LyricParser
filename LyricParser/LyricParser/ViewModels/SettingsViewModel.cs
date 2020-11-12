using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LyricParser.ViewModels
{
    public class SettingsViewModel : DialogBase
    {
        private string _selectedTheme = "Dark";

        private int _maxRetries = 5;
        private int _selectedDebugCategory = 0;

        private bool _debugChecked = false;

        #region Properties

        // String properties
        public string Title {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
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
            LoadTheme();
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

        public bool newSettings = false;

        private void Save()
        {
            newSettings = true;
            bool debugMode = DebugChecked;
            int debugCategory = SelectedDebugCategory;
            MaxRetries = Properties.UserSettings.Default.MaxRetries;
            Properties.UserSettings.Default.ThemePath = SelectedTheme + ".xaml";
            Properties.UserSettings.Default.MaxRetries = MaxRetries;
            Properties.UserSettings.Default.Save();

            CloseDialog(newSettings.ToString());
        }
    }
}
