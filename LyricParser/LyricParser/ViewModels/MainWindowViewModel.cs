using LyricParser.Common;
using LyricParser.Models;
using LyricParser.Resources;
using LyricParser.Services.Interfaces;
using LyricParser.Utils;
using LyricParser.Utils.Parsers.Anime;
using LyricParser.Utils.Parsers.JP;
using LyricParser.Utils.Parsers.Touhou;
using LyricParser.Utils.Parsers.Vocaloid;
using LyricParser.Utils.Parsers.Western;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace LyricParser.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        const int POLLING_SPAN = 1;
        private int MAX_RETRIES = 6;
        private static int retries = 6;
        private static bool paused = false;

        private double zoomValue = 100.0;
        private readonly double defFontSize = 12.0;
        private readonly double zoomStep = 5.0;
        private DispatcherTimer zoomTimer;
        int zoomMode = 0;

        private bool? autoSearch = true;
        private bool initComplete = false;
        private bool isAddingLyrics = false;

        private List<string> rows = new List<string>();
        private List<string> lyrics = new List<string>();

        public Status currentStatus = Status.Standby;
        public Song currentSong;
        public string currentUrl = "";
        public static Player currentPlayer = Player.Winamp;
        public static Category debug_mode = Category.None;
        static Category songCategory = Category.Western;

        const int MAX_ANIME_RETRIES = 10;
        int anime_retry = 0;
        int western_retry = 0;

        private List<Key> keysDown = new List<Key>();

        private string _title = "LyricParser";
        private string _songName = " - ";
        private string _lyrics = "";
        private string _statusText = "Searching...";
        private string _zoomText = "100 %";

        private int _selectedPlayer = 0;

        private bool _autoSearchChecked = true;
        private bool _songEnabled = false;
        private bool _getLyricsEnabled = true;
        private bool _animeRadChecked = false;
        private bool _touhouRadChecked = false;
        private bool _westRadChecked = false;
        private bool _jpRadChecked = false;
        private bool _otherRadChecked = false;

        private double _viewHeight = 691;
        private double _lyricsFontSize = 14;

        //private HistoryEntry _songEntry = new HistoryEntry();

        private Visibility _animeRadVisibility = Visibility.Collapsed;
        private Visibility _touhouRadVisibility = Visibility.Collapsed;
        private Visibility _westRadVisibility = Visibility.Collapsed;
        private Visibility _jpRadVisibility = Visibility.Collapsed;
        private Visibility _otherRadVisibility = Visibility.Collapsed;
        private Visibility _originalLyricsVisibility = Visibility.Visible;
        private Visibility _romajiLyricsVisibility = Visibility.Collapsed;
        private Visibility _englishLyricsVisibility = Visibility.Collapsed;

        #region Public Properties

        // String properties
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string SongName
        {
            get => _songName;
            set => SetProperty(ref _songName, value);
        }
        public string OriginalLyrics
        {
            get => _lyrics;
            set => SetProperty(ref _lyrics, value);
        }
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
        public string ZoomText
        {
            get => _zoomText;
            set => SetProperty(ref _zoomText, value);
        }

        // Int properties
        public int SelectedPlayer
        {
            get => _selectedPlayer;
            set => SetProperty(ref _selectedPlayer, value);
        }

        // Bool properties
        public bool AutoSearchChecked
        {
            get => _autoSearchChecked;
            set => SetProperty(ref _autoSearchChecked, value);
        }
        public bool SongEnabled
        {
            get => _songEnabled;
            set => SetProperty(ref _songEnabled, value);
        }
        public bool GetLyricsEnabled
        {
            get => _getLyricsEnabled;
            set => SetProperty(ref _getLyricsEnabled, value);
        }
        public bool AnimeRadChecked
        {
            get => _animeRadChecked;
            set => SetProperty(ref _animeRadChecked, value);
        }
        public bool TouhouRadChecked
        {
            get => _touhouRadChecked;
            set => SetProperty(ref _touhouRadChecked, value);
        }
        public bool WestRadChecked
        {
            get => _westRadChecked;
            set => SetProperty(ref _westRadChecked, value);
        }
        public bool JpRadChecked
        {
            get => _jpRadChecked;
            set => SetProperty(ref _jpRadChecked, value);
        }
        public bool OtherRadChecked
        {
            get => _otherRadChecked;
            set => SetProperty(ref _otherRadChecked, value);
        }

        // Double properties
        public double ViewHeight
        {
            get => _viewHeight;
            set => SetProperty(ref _viewHeight, value);
        }
        public double LyricsFontSize
        {
            get => _lyricsFontSize;
            set => SetProperty(ref _lyricsFontSize, value);
        }

        // HistoryEntry properties
        //public HistoryEntry SongEntry
        //{
        //    get => _songEntry;
        //    set => SetProperty(ref _songEntry, value);
        //}

        // Visibility properties
        public Visibility AnimeRadVisibility
        {
            get => _animeRadVisibility;
            set => SetProperty(ref _animeRadVisibility, value);
        }
        public Visibility TouhouRadVisibility
        {
            get => _touhouRadVisibility;
            set => SetProperty(ref _touhouRadVisibility, value);
        }
        public Visibility WestRadVisibility
        {
            get => _westRadVisibility;
            set => SetProperty(ref _westRadVisibility, value);
        }
        public Visibility JpRadVisibility
        {
            get => _jpRadVisibility;
            set => SetProperty(ref _jpRadVisibility, value);
        }
        public Visibility OtherRadVisibility
        {
            get => _otherRadVisibility;
            set => SetProperty(ref _otherRadVisibility, value);
        }
        public Visibility OriginalLyricsVisibility
        {
            get => _originalLyricsVisibility;
            set => SetProperty(ref _originalLyricsVisibility, value);
        }
        public Visibility RomajiLyricsVisibility
        {
            get => _romajiLyricsVisibility;
            set => SetProperty(ref _romajiLyricsVisibility, value);
        }
        public Visibility EnglishLyricsVisibility
        {
            get => _englishLyricsVisibility;
            set => SetProperty(ref _englishLyricsVisibility, value);
        }

        #endregion

        #region Commands
        public DelegateCommand GetLyricsCommand { get; }

        public DelegateCommand ViewSizeChangedCommand { get; }
        public DelegateCommand ViewLoadedCommand { get; }
        public DelegateCommand ViewClosingCommand { get; }
        public DelegateCommand <MouseWheelEventArgs> ViewMouseWheelCommand { get; }
        public DelegateCommand <KeyEventArgs> ViewKeyDownCommand { get; }
        public DelegateCommand <KeyEventArgs> ViewKeyUpCommand { get; }

        public DelegateCommand AutoSearchCheckedCommand { get; }
        public DelegateCommand AutoSearchUncheckedCommand { get; }

        public DelegateCommand CategoryRadClickedCommand { get; }

        public DelegateCommand <KeyEventArgs> ZoomKeyDownCommand { get; }
        public DelegateCommand <MouseWheelEventArgs> ZoomMouseWheelCommand { get; }
        public DelegateCommand ZoomLostFocusCommand { get; }
        public DelegateCommand StartZoomInCommand { get; }
        public DelegateCommand StartZoomOutCommand { get; }
        public DelegateCommand StopZoomCommand { get; }

        public DelegateCommand GetCurrentSongCommand { get; }
        public DelegateCommand SearchInBrowserCommand { get; }
        public DelegateCommand ClearSearchHistoryCommand { get; }

        public DelegateCommand OpenEditLyricsCommand { get; }
        public DelegateCommand OpenSettingsCommand { get; }

        #endregion

        #region Services

        private IDialogService _dialogService { get; }
        private IPollingService _pollingService { get; }

        #endregion

        IEventAggregator _eventAggregator { get; }

        // Load settings
        public void LoadSettings()
        {
            MAX_RETRIES = Properties.UserSettings.Default.MaxRetries;
            zoomValue = Properties.Settings.Default.ZoomLevel;
            Zoom(zoomValue);
            retries = MAX_RETRIES;

            CultureInfo newCulture = new CultureInfo(Properties.UserSettings.Default.Locale);
            if (Thread.CurrentThread.CurrentCulture.Name != newCulture.Name) App.ChangeCulture(newCulture);

            LoadTheme();

            if (!Properties.UserSettings.Default.SearchAnime) AnimeRadVisibility = Visibility.Collapsed;
            else AnimeRadVisibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchTouhou) TouhouRadVisibility = Visibility.Collapsed;
            else TouhouRadVisibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchWest) WestRadVisibility = Visibility.Collapsed;
            else WestRadVisibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchJP) JpRadVisibility = Visibility.Collapsed;
            else JpRadVisibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchOther) OtherRadVisibility = Visibility.Collapsed;
            else OtherRadVisibility = Visibility.Visible;

            if (Properties.UserSettings.Default.DebugMode) debug_mode = (Category)Properties.UserSettings.Default.DebugCategory;
            else debug_mode = Category.None;

            if (debug_mode == Category.None) songCategory = (Category)Properties.Settings.Default.LastCategory;

            AutoSearchChecked = Properties.Settings.Default.AutoSearch;

        }

        // Load themes into dictionaries
        public void LoadTheme()
        {
            Uri resUri = new Uri("/Resources/Resources.xaml", UriKind.Relative);
            Uri themeUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Themes\" + Properties.UserSettings.Default.ThemePath);

            var resource = Application.Current.MainWindow.Resources.MergedDictionaries;
            resource.Clear();

            var resDic = new ResourceDictionary();
            var themeDic = new ResourceDictionary();

            resDic.Source = resUri;
            themeDic.Source = themeUri;

            resDic.MergedDictionaries.Add(themeDic);
            resource.Add(resDic);
        }

        public MainWindowViewModel(IEventAggregator ea, IDialogService ds, IPollingService ps)
        {
            _eventAggregator = ea;
            _dialogService = ds;
            _pollingService = ps;
            _pollingService.Span = TimeSpan.FromSeconds(POLLING_SPAN);
            _pollingService.Callback = GetSong;

            GetLyricsCommand = new DelegateCommand(async () => await ExecuteGetLyrics());

            ViewLoadedCommand = new DelegateCommand(OnViewLoaded);
            ViewClosingCommand = new DelegateCommand(OnViewClosing);
            ViewMouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(OnViewMouseWheel);
            ViewKeyDownCommand = new DelegateCommand<KeyEventArgs>(OnViewKeyDown);
            ViewKeyUpCommand = new DelegateCommand<KeyEventArgs>(OnViewKeyUp);

            AutoSearchCheckedCommand = new DelegateCommand(OnAutoSearchChecked);
            AutoSearchUncheckedCommand = new DelegateCommand(OnAutoSearchUnchecked);

            CategoryRadClickedCommand = new DelegateCommand(OnCategoryRadClicked);

            ZoomKeyDownCommand = new DelegateCommand<KeyEventArgs>(OnZoomKeyDown);
            ZoomMouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(OnZoomMouseWheel);
            ZoomLostFocusCommand = new DelegateCommand(OnZoomLostFocus);
            StartZoomInCommand = new DelegateCommand(() => SetZoom(1));
            StartZoomOutCommand = new DelegateCommand(() => SetZoom(-1));
            StopZoomCommand = new DelegateCommand(() => SetZoom(0));

            GetCurrentSongCommand = new DelegateCommand(GetSong);
            SearchInBrowserCommand = new DelegateCommand(SearchInBrowser);
            ClearSearchHistoryCommand = new DelegateCommand(ClearSearchHistory);

            OpenEditLyricsCommand = new DelegateCommand(OpenEditLyrics); // <------- Make async?
            OpenSettingsCommand = new DelegateCommand(OpenSettings); // <------- Make async?

            //HistoryEntry LastSong = DatabaseHandler.GetLastSong();

            //SongEntry = LastSong;

            LoadTheme();
            currentSong = Song.Empty();

            var themes = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory + @"Themes\", "*", SearchOption.TopDirectoryOnly)
                                  .Select(System.IO.Path.GetFileNameWithoutExtension);

            Properties.UserSettings.Default.Themes.Clear();
            Properties.UserSettings.Default.Themes.AddRange(themes.ToArray());
            Properties.UserSettings.Default.Save();

            LoadSettings();

            currentPlayer = (Player)Properties.Settings.Default.LastPlayer;

            switch (songCategory)
            {
                case Category.Anime:
                    AnimeRadChecked = true;
                    break;
                case Category.Touhou:
                    TouhouRadChecked = true;
                    break;
                case Category.Western:
                    WestRadChecked = true;
                    break;
                case Category.JP:
                    JpRadChecked = true;
                    break;
                case Category.Other:
                    OtherRadChecked = true;
                    break;
            }

            zoomTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100),
                IsEnabled = false
            };
            zoomTimer.Tick += ZoomTimer_Tick;

            SetStatus(Status.Standby);
            _pollingService.Start();
            initComplete = true;
        }

        // Send the editied lyrics to the database to be processed
        public bool EditLyrics(string title, string title_en, string artist, string original, Category genre)
        {
            bool success = false;

            List<string> lyrics = new List<string>
            {
                original,
            };

            //success = DatabaseHandler.AddSong(artist, title, title_en, genre, lyrics, DatabaseHandler.LyricsExist(currentSong, false, this));                      <---------- TO BE IMPLEMENTED

            if (currentSong.Artist == artist && currentSong.Title == title)
                SetLyrics(original);

            return success;
        }

        // Show the edited lyrics in the main window
        public void SetLyrics(string content = "")
        {
            lyrics.Clear();
            lyrics.Add(content);

            SetUpTables();
        }

        // Add a new HistoryEntry to the collection, remove last item if count exeeds 20 and remove duplicate if exists
        private void AddHistoryEntry(string data)
        {
            //if (data == " - ") return;
            //if (viewModel.SearchHistory != null && viewModel.SearchHistory.Count > 0)                     <---------- TO BE IMPLEMENTED
            //{
            //    if (viewModel.SearchHistory.Count == 20)
            //    {
            //        viewModel.SearchHistory.RemoveAt(19);
            //    }
            //    if (viewModel.SearchHistory.Any(s => s.Data == data))
            //        viewModel.SearchHistory.Remove(viewModel.SearchHistory.Single(s => s.Data == data));
            //}

            //viewModel.AddHistoryEntry(new HistoryEntry { Data = data });
            //SongEntry = viewModel.SearchHistory.ElementAt(0);
        }

        public async void GetSong()
        {
            autoSearch = AutoSearchChecked;
            Song currentlyPlaying = Song.GetSongInfo(currentPlayer);
            if (autoSearch == true && currentlyPlaying.Title != "Winamp 5.666 Build 3516")
            {
                if ((currentlyPlaying.Title != currentSong.Title && !paused))
                {
                    SetCurrentSong(currentlyPlaying);
                    retries = MAX_RETRIES;

                    await GetLyrics(currentSong.Artist, currentSong.Title);
                }
            }
        }

        async Task ExecuteGetLyrics()
        {
            await GetLyrics(currentSong.Artist, currentSong.Title);
        }

        // Update last song and search history then begin fetching lyrics for the current song
        async Task GetLyrics(string artist, string title)
        {
            CleanUp();
            SetStatus(Status.Searching);
            bool success = false;

            retries = MAX_RETRIES;

            var lyrics = await App.Database.GetLyricsAsync(artist.ToLower(), title.ToLower());
            if(lyrics == null)
            {
                success = await AddLyrics(artist, title);
                if (success)
                    lyrics = await App.Database.GetLyricsAsync(artist.ToLower(), title.ToLower()); // Get lyrics directly from AddLyrics instead?
            }

            if (success)
            {
                SetLyrics(lyrics.Content);
                SetStatus(Status.Done);
            }
            else
                SetStatus(Status.Failed);
        }

        async Task<bool> AddLyrics(string artist, string title)
        {
            if (isAddingLyrics) return false;

            isAddingLyrics = true;
            string content = "";

            SetStatus(Status.Parsing);

            switch (songCategory)
            {
                case Category.Anime:
                    content = await new GendouParser().ParseHtml(artist, title, "& page = " + anime_retry);
                    break;
                case Category.Touhou:
                    content = await new TouhouwikiParser().ParseHtml(artist, title);
                    break;
                case Category.JP:
                    content = await new JlyricParser().ParseHtml(artist, title);
                    break;
                case Category.Western:
                    content = await new MetrolyricsParser().ParseHtml(artist, title);
                    break;
                case Category.Other:
                    content = await new AtwikiParser().ParseHtml(artist, title);
                    break;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                isAddingLyrics = false;
                return false;
            }

            await App.Database.SaveLyricsAsync(new Lyrics()
            {
                Artist = artist.ToLower(),
                Title = title.ToLower(),
                Content = content
            });
            isAddingLyrics = false;

            return true;
        }

        private void SetCurrentSong(Song song)
        {
            currentSong = song;
            SongName = song.Artist + " - " + song.Title;
            Title = SongName;
        }

        // Cleanup lyrics view
        private void CleanUp()
        {
            rows.Clear();
            OriginalLyrics = "";
            lyrics.Clear();
        }

        // Set status of the search
        public void SetStatus(Status status)
        {
            currentStatus = status;
            switch (status)
            {
                case Status.Done:
                    StatusText = LocaleResources.FoundLyrics;
                    break;
                case Status.Searching:
                    StatusText = LocaleResources.Searching;
                    break;
                case Status.Parsing:
                    StatusText = LocaleResources.Parsing;
                    break;
                case Status.Failed:
                    StatusText = LocaleResources.Failed;
                    break;
                case Status.Standby:
                    StatusText = LocaleResources.Standby;
                    break;
            }
        }

        // Set category
        private void SetCategory(Category id)
        {
            if (!initComplete) return;
            songCategory = id;
        }

        // Setup grid that will contain the lyrics
        private void SetUpTables()
        {
            SetStatus(Status.Done);

            OriginalLyrics = "";
            if (lyrics.Count > 0)
            {
                foreach (string str in lyrics)
                {
                    OriginalLyrics += str;
                }
            }
        }

        // Change zoom level ie. change font size
        private void Zoom(double d)
        {
            zoomValue = d > 5 ? d : 5;
            ZoomText = zoomValue.ToString() + " %";

            double newSize = defFontSize / 100 * zoomValue;

            LyricsFontSize = newSize;
        }

        // Change zoom on mouse scroll
        private void OnViewMouseWheel(MouseWheelEventArgs e)
        {
            if (keysDown.Contains(Key.LeftCtrl))
            {
                e.Handled = true;
                double x = e.Delta > 0 ? zoomStep : -zoomStep;
                Zoom(zoomValue + x);
            }
        }

        private void OnCategoryRadClicked()
        {
            if (AnimeRadChecked) SetCategory(Category.Anime);
            else if (TouhouRadChecked) SetCategory(Category.Touhou);
            else if (WestRadChecked) SetCategory(Category.Western);
            else if (JpRadChecked) SetCategory(Category.JP);
            else if (OtherRadChecked) SetCategory(Category.Other);

            retries = MAX_RETRIES;
        }

        private void OnViewLoaded()
        {
        }
        private void OnAutoSearchChecked()
        {
            SongEnabled = false;
            autoSearch = true;
            Properties.Settings.Default.AutoSearch = true;
            Properties.Settings.Default.Save();
        }

        private void OnAutoSearchUnchecked()
        {
            GetLyricsEnabled = true;
            SongEnabled = true;
            autoSearch = false;
            Properties.Settings.Default.AutoSearch = false;
            Properties.Settings.Default.Save();
        }
        private void OnViewKeyDown(KeyEventArgs e)
        {
            if (!keysDown.Contains(e.Key))
            {
                keysDown.Add(e.Key);
            }
        }

        private void OnViewKeyUp(KeyEventArgs e)
        {
            keysDown.Remove(e.Key);
        }

        private void OnViewClosing()
        {
            Properties.Settings.Default.LastCategory = (int)songCategory;
            Properties.Settings.Default.LastPlayer = SelectedPlayer;
            Properties.Settings.Default.ZoomLevel = zoomValue;
            Properties.Settings.Default.AutoSearch = AutoSearchChecked;

            //if (WindowState == WindowState.Maximized)                      <---------- TO BE IMPLEMENTED
            //{
            //    Properties.Settings.Default.Top = RestoreBounds.Top;
            //    Properties.Settings.Default.Left = RestoreBounds.Left;
            //    Properties.Settings.Default.Height = RestoreBounds.Height;
            //    Properties.Settings.Default.Width = RestoreBounds.Width;
            //    Properties.Settings.Default.Maximized = true;
            //}
            //else
            //{
            //    Properties.Settings.Default.Top = this.Top;
            //    Properties.Settings.Default.Left = this.Left;
            //    Properties.Settings.Default.Height = this.Height;
            //    Properties.Settings.Default.Width = this.Width;
            //    Properties.Settings.Default.Maximized = false;
            //}
            Properties.Settings.Default.Save();
        }

        private void SearchInBrowser()
        {
            if (currentUrl != "") Process.Start(currentUrl);
        }

        private void ClearSearchHistory()
        {
            //viewModel.SearchHistory.Clear();                     <---------- TO BE IMPLEMENTED
        }
        private void OpenEditLyrics()
        {
            //EditLyricsView el = new EditLyricsView(this);
            //el.ShowDialog();
        }
        private void OpenSettings()
        {
            _dialogService.ShowDialog(nameof(SettingsView), new DialogParameters(), r => 
            {
                if (r.Result == ButtonResult.OK)
                    SettingsClosed();
            });
        }

        private void SettingsClosed()
        {
            LoadSettings();
        }

        private void OnZoomLostFocus()
        {
            Zoom(double.Parse(new string(ZoomText.TakeWhile(Char.IsDigit).ToArray())));
        }

        private void OnZoomKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Zoom(double.Parse(new string(ZoomText.TakeWhile(Char.IsDigit).ToArray())));
            }
        }

        private void OnZoomMouseWheel(MouseWheelEventArgs e)
        {
            //if (ZoomTxt.IsFocused)                      <---------- TO BE IMPLEMENTED
            //{
            //    bool inc = e.Delta > 0;
            //    double step = 10;
            //    if (inc) zoomValue += step;
            //    else if (!inc && zoomValue - step >= 0) zoomValue -= step;
            //    else zoomValue = 0;

            //    ZoomTxt.Text = zoomValue.ToString();
            //}
        }

        private void ZoomTimer_Tick(object sender, EventArgs e)
        {
            if (zoomMode != 0)
            {
                Zoom(zoomValue + zoomStep * zoomMode);
            }
        }

        private void SetZoom(int mode)
        {
            if(mode == 0)
            {
                double targetZoom = zoomValue + zoomStep * zoomMode;
                zoomMode = mode;
                zoomTimer.Stop();
                Zoom(targetZoom);
            }
            else
            {
                zoomMode = mode;
                zoomTimer.Start();
            }
        }
    }
}
