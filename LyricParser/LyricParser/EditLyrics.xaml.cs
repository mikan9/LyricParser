using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using System.Windows.Threading;

namespace LyricParser
{
    /// <summary>
    /// Interaction logic for EditLyrics.xaml
    /// </summary>
    public partial class EditLyrics : Window
    {
        private MainWindow main;

        double heightDiff = 100 + 20;

        List<Key> keysDown = new List<Key>();
        private double zoomValue = 100.0;
        private double defFontSize = 12.0;
        private double zoomStep = 5.0;
        DispatcherTimer zoomTimer;
        int zoomMode = 0;

        bool initComplete = false;

        Lyric currentLyric;

        public void LoadTheme()
        {
            Uri resUri = new Uri("/Resources.xaml", UriKind.Relative);
            Uri themeUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Themes\" + Properties.UserSettings.Default.ThemePath);

            var resource = this.Resources.MergedDictionaries;
            resource.Clear();

            var resDic = new ResourceDictionary();
            var themeDic = new ResourceDictionary();

            resDic.Source = resUri;
            themeDic.Source = themeUri;

            resDic.MergedDictionaries.Add(themeDic);
            resource.Add(resDic);
        }

        public EditLyrics(MainWindow main)
        {
            InitializeComponent();
            this.main = main;
            this.Title = LocaleResources.EditWindowTitle + " | " + main.currentSong.Artist + " - " + main.currentSong.Title;
            DatabaseHandler.LoadLyrics();
            foreach (Lyric l in LyricHandler.LoadLyrics())
            {
                SavedLyricsBox.Items.Add(l.artist + " - " + l.title);
            }
            //if (SavedLyricsBox.Items.Count > 0) SavedLyricsBox.SelectedIndex = 0;

            LoadTheme();

            currentLyric = LyricHandler.CreateLyric(main.currentSong.Artist, main.currentSong.Title, main.currentSong.Genre,
                                                    main.originalTxt.Text, main.romajiTxt.Text, main.englishTxt.Text);

            artistBox.Text = currentLyric.artist;
            titleBox.Text = currentLyric.title;
            genreBox.SelectedIndex = (int)currentLyric.genre;

            SetUpTables();

            zoomTimer = new DispatcherTimer();
            zoomTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            zoomTimer.IsEnabled = false;
            zoomTimer.Tick += ZoomTimer_Tick;  
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            initComplete = true;
            origCheck.IsChecked = Properties.Settings.Default.EditOriginal;
            romajiCheck.IsChecked = Properties.Settings.Default.EditRomaji;
            engCheck.IsChecked = Properties.Settings.Default.EditEnglish;
            SetUpTables();
        }

        private void SetUpTables()
        {
            Properties.Settings.Default.Save();
            originalTxt.Visibility = System.Windows.Visibility.Collapsed;
            originalLbl.Visibility = originalTxt.Visibility;
            romajiTxt.Visibility = System.Windows.Visibility.Collapsed;
            romajiLbl.Visibility = romajiTxt.Visibility;
            englishTxt.Visibility = System.Windows.Visibility.Collapsed;
            englishLbl.Visibility = englishTxt.Visibility;

            contentGrid.ColumnDefinitions.Clear();
            headerGrid.ColumnDefinitions.Clear();

            bool bShowOrig = false;
            bool bShowRom = false;
            bool bShowEng = false;

            bShowOrig = Properties.Settings.Default.EditOriginal;
            bShowRom = Properties.Settings.Default.EditRomaji;
            bShowEng = Properties.Settings.Default.EditEnglish;

            if (bShowOrig)
            {
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition());

                Grid.SetColumn(originalTxt, 0);
                Grid.SetColumn(originalLbl, 0);

                originalTxt.Visibility = System.Windows.Visibility.Visible;
                originalLbl.Visibility = originalTxt.Visibility;

                if (bShowRom)
                {
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition());

                    Grid.SetColumn(romajiLbl, 1);
                    Grid.SetColumn(romajiTxt, 1);

                    romajiTxt.Visibility = System.Windows.Visibility.Visible;
                    romajiLbl.Visibility = romajiTxt.Visibility;
                }
                if (bShowEng)
                {
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition());

                    int col = 2;
                    if (!bShowRom) col = 1;

                    Grid.SetColumn(englishLbl, col);
                    Grid.SetColumn(englishTxt, col);

                    englishTxt.Visibility = System.Windows.Visibility.Visible;
                    englishLbl.Visibility = englishTxt.Visibility;
                }
            }

            //artistBox.Text = currentLyric.artist;
            //titleBox.Text = currentLyric.title;
            //genreBox.SelectedIndex = (int)currentLyric.genre;
            originalTxt.Text = currentLyric.original;
            romajiTxt.Text = currentLyric.romaji;
            englishTxt.Text = currentLyric.translation;

            return;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double newHeight = this.ActualHeight - heightDiff;
            if (newHeight > 0)
            {
                originalTxt.Height = newHeight;
                romajiTxt.Height = newHeight;
                englishTxt.Height = newHeight;
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            main.EditLyrics(titleBox.Text, "", artistBox.Text, originalTxt.Text, romajiTxt.Text, englishTxt.Text, (Category)genreBox.SelectedIndex);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!keysDown.Contains(e.Key))
            {
                keysDown.Add(e.Key);
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            keysDown.Remove(e.Key);
        }

        private void zoom(double d)
        {
            zoomValue = d > 5 ? d : 5;
            zoomTxt.Text = zoomValue.ToString() + " %";

            double newSize = defFontSize / 100 * zoomValue;

            romajiTxt.FontSize = newSize;
            englishTxt.FontSize = romajiTxt.FontSize;
            originalTxt.FontSize = romajiTxt.FontSize;
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (keysDown.Contains(Key.LeftCtrl))
            {
                e.Handled = true;
                double x = e.Delta > 0 ? zoomStep : -zoomStep;
                zoom(zoomValue + x);
            }
        }

        private void zoomTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            zoom(double.Parse(new string(zoomTxt.Text.TakeWhile(Char.IsDigit).ToArray())));
        }

        private void zoomTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                zoom(double.Parse(new string(zoomTxt.Text.TakeWhile(Char.IsDigit).ToArray())));
            }
        }

        private void zoomTxt_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (zoomTxt.IsFocused)
            {
                bool inc = e.Delta > 0;
                double step = 10;
                if (inc) zoomValue += step;
                else if (!inc && zoomValue - step >= 0) zoomValue -= step;
                else zoomValue = 0;

                zoomTxt.Text = zoomValue.ToString();
            }
        }

        private void ZoomTimer_Tick(object sender, EventArgs e)
        {
            if (zoomMode != 0)
            {
                zoom(zoomValue + zoomStep * zoomMode);
            }
        }

        private void enlargeBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            zoomMode = 1;
            zoomTimer.Start();
        }

        private void enlargeBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            zoomMode = 0;
            zoomTimer.Stop();
            zoom(zoomValue + zoomStep);
        }

        private void shrinkBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            zoomMode = -1;
            zoomTimer.Start();
        }

        private void shrinkBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            zoomMode = 0;
            zoomTimer.Stop();
            zoom(zoomValue - zoomStep);
        }

        private void origCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (initComplete)
            {
                originalLbl.Visibility = Visibility.Visible;
                originalTxt.Visibility = originalLbl.Visibility;
                SetUpTables();
            }
        }

        private void romajiCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (initComplete)
            {
                romajiLbl.Visibility = Visibility.Visible;
                romajiTxt.Visibility = romajiLbl.Visibility;
                SetUpTables();
            }
        }

        private void engCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (initComplete)
            {
                englishLbl.Visibility = Visibility.Visible;
                englishTxt.Visibility = englishLbl.Visibility;
                SetUpTables();
            }
        }

        private void origCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initComplete)
            {
                originalLbl.Visibility = Visibility.Collapsed;
                originalTxt.Visibility = originalLbl.Visibility;
                SetUpTables();
            }
        }

        private void romajiCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initComplete)
            {
                romajiLbl.Visibility = Visibility.Collapsed;
                romajiTxt.Visibility = romajiLbl.Visibility;
                SetUpTables();
            }
        }

        private void engCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            if (initComplete)
            {
                englishLbl.Visibility = Visibility.Collapsed;
                englishTxt.Visibility = englishLbl.Visibility;
                SetUpTables();
            }
        }

        private void SavedLyricsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentLyric = LyricHandler.LoadLyrics().ElementAt(SavedLyricsBox.SelectedIndex);
            SetUpTables();
        }
    }
}
