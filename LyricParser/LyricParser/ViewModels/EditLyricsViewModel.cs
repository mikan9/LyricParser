using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LyricParser.ViewModels
{
    public class EditLyricsViewModel : BindableBase, IDialogAware
    {
        private MainWindowView main;
        private readonly double heightDiff = 100 + 20;
        private List<Key> keysDown = new List<Key>();
        private double zoomValue = 100.0;
        private readonly double defFontSize = 12.0;
        private readonly double zoomStep = 5.0;
        private DispatcherTimer zoomTimer;
        int zoomMode = 0;

        bool initComplete = false;

        private string _title = "Edit lyrics";

        public event Action<IDialogResult> RequestClose;


        // String properties
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // Load theme into dictionaries
        //public void LoadTheme()
        //{
        //    Uri resUri = new Uri("/Resources.xaml", UriKind.Relative);
        //    Uri themeUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Themes\" + Properties.UserSettings.Default.ThemePath);

        //    var resource = this.Resources.MergedDictionaries;
        //    resource.Clear();

        //    var resDic = new ResourceDictionary();
        //    var themeDic = new ResourceDictionary();

        //    resDic.Source = resUri;
        //    themeDic.Source = themeUri;

        //    resDic.MergedDictionaries.Add(themeDic);
        //    resource.Add(resDic);
        //}

        public EditLyricsViewModel()
        {
            //ViewTitle = LocaleResources.EditWindowTitle + " | " + main.currentSong.Artist + " - " + main.currentSong.Title;                      <---------- TO BE IMPLEMENTED
            //LoadLyrics();
            //LoadTheme();

            //currentLyric = LyricHandler.CreateLyric(main.currentSong.Artist, main.currentSong.Title, main.currentSong.Genre,
            //                                        main.OriginalTxt.Text, main.RomajiTxt.Text, main.EnglishTxt.Text);

            //ArtistBox.Text = currentLyric.artist;
            //TitleBox.Text = currentLyric.title;
            //GenreBox.SelectedIndex = (int)currentLyric.genre;

            //SetUpTables();

            //zoomTimer = new DispatcherTimer
            //{
            //    Interval = new TimeSpan(0, 0, 0, 0, 100),
            //    IsEnabled = false
            //};
            //zoomTimer.Tick += ZoomTimer_Tick;
        }

        public bool CanCloseDialog()
        {
            throw new NotImplementedException();
        }

        public void OnDialogClosed()
        {
            throw new NotImplementedException();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            throw new NotImplementedException();
        }

        // Load lyrics from database
        //private void LoadLyrics()
        //{
        //    DatabaseHandler.LoadLyrics();
        //    SavedLyricsBox.Items.Clear();
        //    foreach (Lyric l in LyricHandler.GetLyrics())
        //    {
        //        SavedLyricsBox.Items.Add(l.artist + " - " + l.title);
        //    }
        //}

        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    initComplete = true;
        //    OrigCheck.IsChecked = Properties.Settings.Default.EditOriginal;
        //    RomajiCheck.IsChecked = Properties.Settings.Default.EditRomaji;
        //    EngCheck.IsChecked = Properties.Settings.Default.EditEnglish;
        //    SetUpTables();
        //}

        //// Setup grid that will contain the lyrics
        //private void SetUpTables()
        //{
        //    Properties.Settings.Default.Save();
        //    OriginalTxt.Visibility = System.Windows.Visibility.Collapsed;
        //    OriginalLbl.Visibility = OriginalTxt.Visibility;
        //    RomajiTxt.Visibility = System.Windows.Visibility.Collapsed;
        //    RomajiLbl.Visibility = RomajiTxt.Visibility;
        //    EnglishTxt.Visibility = System.Windows.Visibility.Collapsed;
        //    EnglishLbl.Visibility = EnglishTxt.Visibility;

        //    ContentGrid.ColumnDefinitions.Clear();
        //    HeaderGrid.ColumnDefinitions.Clear();

        //    bool bShowOrig = Properties.Settings.Default.EditOriginal;
        //    bool bShowRom = Properties.Settings.Default.EditRomaji;
        //    bool bShowEng = Properties.Settings.Default.EditEnglish;

        //    if (bShowOrig)
        //    {
        //        ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
        //        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

        //        Grid.SetColumn(OriginalTxt, 0);
        //        Grid.SetColumn(OriginalLbl, 0);

        //        OriginalTxt.Visibility = System.Windows.Visibility.Visible;
        //        OriginalLbl.Visibility = OriginalTxt.Visibility;

        //        if (bShowRom)
        //        {
        //            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
        //            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

        //            Grid.SetColumn(RomajiLbl, 1);
        //            Grid.SetColumn(RomajiTxt, 1);

        //            RomajiTxt.Visibility = System.Windows.Visibility.Visible;
        //            RomajiLbl.Visibility = RomajiTxt.Visibility;
        //        }
        //        if (bShowEng)
        //        {
        //            ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
        //            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

        //            int col = 2;
        //            if (!bShowRom) col = 1;

        //            Grid.SetColumn(EnglishLbl, col);
        //            Grid.SetColumn(EnglishTxt, col);

        //            EnglishTxt.Visibility = System.Windows.Visibility.Visible;
        //            EnglishLbl.Visibility = EnglishTxt.Visibility;
        //        }
        //    }

        //    OriginalTxt.Text = currentLyric.original;
        //    RomajiTxt.Text = currentLyric.romaji;
        //    EnglishTxt.Text = currentLyric.translation;

        //    return;
        //}

        //// Change grid height relative to window height
        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    double newHeight = this.ActualHeight - heightDiff;
        //    if (newHeight > 0)
        //    {
        //        OriginalTxt.Height = newHeight;
        //        RomajiTxt.Height = newHeight;
        //        EnglishTxt.Height = newHeight;
        //    }
        //}

        //// Save edited lyrics to the database, if successful load the lyrics
        //private void SaveBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    StatusTxt.Text = LocaleResources.Saving;
        //    bool success = main.EditLyrics(TitleBox.Text, "", ArtistBox.Text, OriginalTxt.Text, RomajiTxt.Text, EnglishTxt.Text, (Category)GenreBox.SelectedIndex);
        //    if (success)
        //    {
        //        LoadLyrics();
        //        StatusTxt.Text = LocaleResources.SaveSuccessful;
        //    }
        //    else StatusTxt.Text = LocaleResources.SaveFailed;
        //}

        //private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (!keysDown.Contains(e.Key))
        //    {
        //        keysDown.Add(e.Key);
        //    }
        //}

        //private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        //{
        //    keysDown.Remove(e.Key);
        //}

        //private void Zoom(double d)
        //{
        //    zoomValue = d > 5 ? d : 5;
        //    ZoomTxt.Text = zoomValue.ToString() + " %";

        //    double newSize = defFontSize / 100 * zoomValue;

        //    RomajiTxt.FontSize = newSize;
        //    EnglishTxt.FontSize = RomajiTxt.FontSize;
        //    OriginalTxt.FontSize = RomajiTxt.FontSize;
        //}

        //private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (keysDown.Contains(Key.LeftCtrl))
        //    {
        //        e.Handled = true;
        //        double x = e.Delta > 0 ? zoomStep : -zoomStep;
        //        Zoom(zoomValue + x);
        //    }
        //}

        //private void ZoomTxt_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    Zoom(double.Parse(new string(ZoomTxt.Text.TakeWhile(Char.IsDigit).ToArray())));
        //}

        //private void ZoomTxt_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        Zoom(double.Parse(new string(ZoomTxt.Text.TakeWhile(Char.IsDigit).ToArray())));
        //    }
        //}

        //private void ZoomTxt_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (ZoomTxt.IsFocused)
        //    {
        //        bool inc = e.Delta > 0;
        //        double step = 10;
        //        if (inc) zoomValue += step;
        //        else if (!inc && zoomValue - step >= 0) zoomValue -= step;
        //        else zoomValue = 0;

        //        ZoomTxt.Text = zoomValue.ToString();
        //    }
        //}

        //private void ZoomTimer_Tick(object sender, EventArgs e)
        //{
        //    if (zoomMode != 0)
        //    {
        //        Zoom(zoomValue + zoomStep * zoomMode);
        //    }
        //}

        //private void EnlargeBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    zoomMode = 1;
        //    zoomTimer.Start();
        //}

        //private void EnlargeBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    zoomMode = 0;
        //    zoomTimer.Stop();
        //    Zoom(zoomValue + zoomStep);
        //}

        //private void ShrinkBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    zoomMode = -1;
        //    zoomTimer.Start();
        //}

        //private void ShrinkBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    zoomMode = 0;
        //    zoomTimer.Stop();
        //    Zoom(zoomValue - zoomStep);
        //}

        //private void OrigCheck_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (initComplete)
        //    {
        //        OriginalLbl.Visibility = Visibility.Visible;
        //        OriginalTxt.Visibility = OriginalLbl.Visibility;
        //        SetUpTables();
        //    }
        //}

        //private void RomajiCheck_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (initComplete)
        //    {
        //        RomajiLbl.Visibility = Visibility.Visible;
        //        RomajiTxt.Visibility = RomajiLbl.Visibility;
        //        SetUpTables();
        //    }
        //}

        //private void EngCheck_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (initComplete)
        //    {
        //        EnglishLbl.Visibility = Visibility.Visible;
        //        EnglishTxt.Visibility = EnglishLbl.Visibility;
        //        SetUpTables();
        //    }
        //}

        //private void OrigCheck_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (initComplete)
        //    {
        //        OriginalLbl.Visibility = Visibility.Collapsed;
        //        OriginalTxt.Visibility = OriginalLbl.Visibility;
        //        SetUpTables();
        //    }
        //}

        //private void RomajiCheck_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (initComplete)
        //    {
        //        RomajiLbl.Visibility = Visibility.Collapsed;
        //        RomajiTxt.Visibility = RomajiLbl.Visibility;
        //        SetUpTables();
        //    }
        //}

        //private void EngCheck_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (initComplete)
        //    {
        //        EnglishLbl.Visibility = Visibility.Collapsed;
        //        EnglishTxt.Visibility = EnglishLbl.Visibility;
        //        SetUpTables();
        //    }
        //}

        //private void SavedLyricsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (e.AddedItems.Count > 0)
        //    {
        //        Trace.WriteLine(SavedLyricsBox.SelectedIndex + " - Count: " + LyricHandler.GetLyrics().Count);
        //        if (SavedLyricsBox.SelectedIndex < 0) return;
        //        currentLyric = LyricHandler.GetLyrics().ElementAt(SavedLyricsBox.SelectedIndex);
        //        ArtistBox.Text = currentLyric.artist;
        //        TitleBox.Text = currentLyric.title;
        //        SetUpTables();
        //    }
        //}
    }
}
