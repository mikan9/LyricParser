using LyricParser.Common;
using LyricParser.Extensions;
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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media.Control;

namespace LyricParser.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private int MAX_RETRIES = 6;
        private static int retries = 6;
        private static bool paused = false;
        private int getLyricsDueTime = 3000;
        private bool isGetLyricsTimerStarted = false;
        private Timer getLyricsTimer;

        private readonly double defFontSize = 16.0;
        private readonly double zoomStep = 25.0;
        private readonly int zoomLevels = 8;

        private bool? autoSearch = true;
        private bool initComplete = false;
        private bool isAddingLyrics = false;

        private List<string> rows = new List<string>();
        private List<string> lyrics = new List<string>();

        public Status currentStatus = Status.Standby;
        public Song currentSong;
        public Lyrics currentLyrics = Lyrics.Empty();
        public string currentUrl = "";
        public static Player currentPlayer = Player.Winamp;
        public static Category debug_mode = Category.None;
        static Category songCategory = Category.Western;

        const int MAX_ANIME_RETRIES = 10;
        int anime_retry = 0;

        private List<Key> keysDown = new List<Key>();

        private int currentSessionIndex = 0;
        private GlobalSystemMediaTransportControlsSessionManager sessionManager;
        private GlobalSystemMediaTransportControlsSession currentSession;
        private List<GlobalSystemMediaTransportControlsSession> sessions;

        private string _title = "LyricParser";
        private string _songName = " - ";
        private string _songArtist = "";
        private string _songTitle = "";
        private string _lyrics = "";
        private string _statusText = "Searching...";
        private string _showHideInfoRightText = "⏵";

        private int _selectedPlayer = 0;
        private int _zoomSelectionIndex = 3;

        private bool _autoSearchChecked = true;
        private bool _getLyricsEnabled = true;
        private bool _animeRadChecked = false;
        private bool _touhouRadChecked = false;
        private bool _westRadChecked = false;
        private bool _jpRadChecked = false;
        private bool _otherRadChecked = false;

        private double _viewHeight = 691;
        private double _lyricsFontSize = 14;

        private string _songEntry = " - ";
        //private HistoryEntry _songEntry = new HistoryEntry();

        private Visibility _animeRadVisibility = Visibility.Collapsed;
        private Visibility _touhouRadVisibility = Visibility.Collapsed;
        private Visibility _westRadVisibility = Visibility.Collapsed;
        private Visibility _jpRadVisibility = Visibility.Collapsed;
        private Visibility _otherRadVisibility = Visibility.Collapsed;
        private Visibility _originalLyricsVisibility = Visibility.Visible;
        private Visibility _romajiLyricsVisibility = Visibility.Collapsed;
        private Visibility _englishLyricsVisibility = Visibility.Collapsed;
        private Visibility _infoRightVisibility = Visibility.Visible;
        private Visibility _changeSessionButtonVisibility = Visibility.Collapsed;

        private ImageSource _thumbnail = null;

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
        public string SongArtist
        {
            get => _songArtist;
            set => SetProperty(ref _songArtist, value);
        }
        public string SongTitle
        {
            get => _songTitle;
            set => SetProperty(ref _songTitle, value);
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
        public string ShowHideInfoRightText
        {
            get => _showHideInfoRightText;
            set => SetProperty(ref _showHideInfoRightText, value);
        }

        // Int properties
        public int SelectedPlayer
        {
            get => _selectedPlayer;
            set => SetProperty(ref _selectedPlayer, value);
        }
        public int ZoomSelectionIndex
        {
            get => _zoomSelectionIndex;
            set => SetProperty(ref _zoomSelectionIndex, value);
        }

        // Bool properties
        public bool AutoSearchChecked
        {
            get => _autoSearchChecked;
            set => SetProperty(ref _autoSearchChecked, value);
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
        public string SongEntry
        {
            get => _songEntry;
            set => SetProperty(ref _songEntry, value);
        }

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
        public Visibility InfoRightVisibility
        {
            get => _infoRightVisibility;
            set => SetProperty(ref _infoRightVisibility, value);
        }
        public Visibility ChangeSessionButtonVisibility
        {
            get => _changeSessionButtonVisibility;
            set => SetProperty(ref _changeSessionButtonVisibility, value);
        }

        // ImageSource properties
        public ImageSource Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        #endregion

        #region Commands
        public DelegateCommand GetLyricsCommand { get; }
        public DelegateCommand OverwriteLyricsCommand { get; }
        public DelegateCommand ChangeSessionCommand { get; }
        public DelegateCommand ViewSizeChangedCommand { get; }
        public DelegateCommand ViewLoadedCommand { get; }
        public DelegateCommand ViewClosingCommand { get; }
        public DelegateCommand <MouseWheelEventArgs> ViewMouseWheelCommand { get; }
        public DelegateCommand <KeyEventArgs> ViewKeyDownCommand { get; }
        public DelegateCommand <KeyEventArgs> ViewKeyUpCommand { get; }

        public DelegateCommand AutoSearchCheckedCommand { get; }
        public DelegateCommand AutoSearchUncheckedCommand { get; }

        public DelegateCommand CategoryRadClickedCommand { get; }

        public DelegateCommand <SelectionChangedEventArgs> ZoomSelectionChangedCommand { get; }

        public DelegateCommand GetCurrentSongCommand { get; }
        public DelegateCommand SearchInBrowserCommand { get; }
        public DelegateCommand ClearSearchHistoryCommand { get; }

        public DelegateCommand OpenEditLyricsCommand { get; }
        public DelegateCommand OpenSettingsCommand { get; }
        public DelegateCommand ShowHideInfoRightCommand { get; }

        #endregion

        #region Services

        private IDialogService _dialogService { get; }

        #endregion

        IEventAggregator _eventAggregator { get; }

        // Load settings
        public void LoadSettings()
        {
            MAX_RETRIES = Properties.UserSettings.Default.MaxRetries;
            ZoomSelectionIndex = Properties.Settings.Default.ZoomIndex;
            SetFontSize();
            ShowInfoRight(Properties.Settings.Default.ShowInfoRight);

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

        public MainWindowViewModel(IEventAggregator ea, IDialogService ds)
        {
            _eventAggregator = ea;
            _dialogService = ds;

            GetLyricsCommand = new DelegateCommand(async () => await ExecuteGetLyrics());

            OverwriteLyricsCommand = new DelegateCommand(async () => await OverwriteLyrics());
            ChangeSessionCommand = new DelegateCommand(async () => await ChangeSession());

            ViewLoadedCommand = new DelegateCommand(OnViewLoaded);
            ViewClosingCommand = new DelegateCommand(OnViewClosing);
            ViewMouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(OnViewMouseWheel);
            ViewKeyDownCommand = new DelegateCommand<KeyEventArgs>(OnViewKeyDown);
            ViewKeyUpCommand = new DelegateCommand<KeyEventArgs>(OnViewKeyUp);

            AutoSearchCheckedCommand = new DelegateCommand(OnAutoSearchChecked);
            AutoSearchUncheckedCommand = new DelegateCommand(OnAutoSearchUnchecked);

            CategoryRadClickedCommand = new DelegateCommand(OnCategoryRadClicked);

            ZoomSelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(ZoomSelectionChanged);

            GetCurrentSongCommand = new DelegateCommand(GetCurrentSong);
            SearchInBrowserCommand = new DelegateCommand(SearchInBrowser);
            ClearSearchHistoryCommand = new DelegateCommand(ClearSearchHistory);

            OpenEditLyricsCommand = new DelegateCommand(OpenEditLyrics); // <------- Make async?
            OpenSettingsCommand = new DelegateCommand(OpenSettings); // <------- Make async?

            ShowHideInfoRightCommand = new DelegateCommand(ToggleInfoRight);

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

            SetStatus(Status.Standby);
            initComplete = true;

            GetSessionManager();
        }

        private async void GetSessionManager()
        {
            sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
            sessionManager.SessionsChanged += SessionManager_SessionsChanged;
            SetSessions();
            await SetCurrentSession();
        }

        private void SessionManager_SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            SetSessions();
        }

        private async void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            await SetCurrentSession();
        }

        private async void CurrentlyPlayingUpdated(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await GetCurrentlyPlaying();
        }

        private async void SetSessions()
        {
            sessions = sessionManager.GetSessions().ToList();
            if (currentSession != null && currentSession.SourceAppUserModelId != sessionManager.GetCurrentSession().SourceAppUserModelId)
                await SetCurrentSession();
            UpdateChangeSessionButtonVisiblity();
        }

        private int GetSessionIndex(GlobalSystemMediaTransportControlsSession session)
        {
            return sessions.FindIndex(x => x.SourceAppUserModelId == session.SourceAppUserModelId);
        }

        private async Task SetSession(int index)
        {
            currentSession = sessions[index];
            currentSessionIndex = index;

            await GetCurrentlyPlaying();
        }

        private void UpdateChangeSessionButtonVisiblity()
        {
            ChangeSessionButtonVisibility = sessions.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task SetCurrentSession()
        {
            currentSession = sessionManager.GetCurrentSession();
            currentSessionIndex = GetSessionIndex(currentSession);

            if (currentSession == null) return;
            currentSession.MediaPropertiesChanged += CurrentlyPlayingUpdated;

            await GetCurrentlyPlaying();
        }

        private async Task ChangeSession()
        {
            currentSessionIndex = GetSessionIndex(currentSession);
            int sessionCount = sessions.Count;
            int nextIndex = currentSessionIndex + 1 < sessionCount ? currentSessionIndex + 1 : 0;

            await SetSession(nextIndex);
        }

        private async Task GetCurrentlyPlaying()
        {
            if (currentSession != null)
            {
                Song currentlyPlaying = await GetSongFromSession(currentSession);

                if (!String.IsNullOrWhiteSpace(currentlyPlaying.Artist) && autoSearch == true)
                {
                    if ((currentlyPlaying.Title != currentSong.Title && !paused))
                    {
                        SetCurrentSong(currentlyPlaying);
                        retries = MAX_RETRIES;

                        await GetLyrics(currentSong.Artist, currentSong.Title);

                        // Make sure lyrics for the currently playing song is fetched
                        if (isGetLyricsTimerStarted)
                            getLyricsTimer.Dispose();

                        getLyricsTimer = new Timer(QueueGetLyrics, currentlyPlaying, getLyricsDueTime, Timeout.Infinite);
                        isGetLyricsTimerStarted = true;
                    }
                }
            }
        }

        public void GetCurrentSong()
        {
            SongEntry = SongName;
        }

        private async void QueueGetLyrics(object obj)
        {
            Song song = (Song)obj;

            if (song.Title != currentSong.Title)
            {
                retries = MAX_RETRIES;

                await GetLyrics(currentSong.Artist, currentSong.Title);
                isGetLyricsTimerStarted = false;
            }
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

        public async Task<Song> GetSongFromSession(GlobalSystemMediaTransportControlsSession session)
        {
            autoSearch = AutoSearchChecked;
            Song currentlyPlaying = Song.Empty();
            var mediaProperties = await session.TryGetMediaPropertiesAsync();

            if (mediaProperties != null && mediaProperties.Title.Length > 0)
            {
                currentlyPlaying.Artist = mediaProperties.Artist;
                currentlyPlaying.Title = mediaProperties.Title;

                var thumbnail = mediaProperties.Thumbnail;

                if (thumbnail != null)
                {
                    var stream = await mediaProperties.Thumbnail.OpenReadAsync();

                    using (StreamReader sr = new StreamReader(stream.AsStream()))
                    {

                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = sr.BaseStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        currentlyPlaying.Thumbnail = bitmap;
                    }
                }
            }
            return currentlyPlaying;
        }

        private void SetCurrentSong(Song song)
        {
            if (song.Title == "Null") return;

            currentSong = song;
            SongName = song.Artist + " - " + song.Title;
            Title = SongName;
            SongArtist = song.Artist;
            SongTitle = song.Title;
            Thumbnail = song.Thumbnail;
        }

        private async Task ExecuteGetLyrics()
        {
            if (AutoSearchChecked == true)
                await GetLyrics(currentSong.Artist, currentSong.Title);
            else
            {
                string[] artistTitle = Song.FromString(SongEntry);
                await GetLyrics(artistTitle[0].Trim(), artistTitle[1].Trim());
            }
        }

        private async Task GetLyrics(string artist, string title)
        {
            CleanUp();
            SetStatus(Status.Searching);
            bool success = true;

            retries = MAX_RETRIES;

            Lyrics lyrics = AutoSearchChecked ? await App.Database.GetLyricsAsync(artist.ToLower(), title.ToLower()) : null;

            if(lyrics == null)
            {
                (success, lyrics) = await AddLyrics(artist, title);
            }

            if (success)
            {
                SetLyrics(lyrics.Content);
                SetStatus(Status.Done);
            }
            else
                SetStatus(Status.Failed);

            currentLyrics = lyrics;
        }

        private async Task<(bool, Lyrics)> AddLyrics(string artist, string title)
        {
            if (isAddingLyrics) return (false, null);

            isAddingLyrics = true;
            string content = "";

            SetStatus(Status.Parsing);

            string _title = title.ToLower().Trim().Normalize();
            string _artist = artist.ToLower().Trim().Normalize();

            switch (songCategory)
            {
                case Category.Anime:
                    content = await new GendouParser().ParseHtml(_artist, _title, "&page=" + anime_retry);
                    break;
                case Category.Touhou:
                    content = await new TouhouwikiParser().ParseHtml(_artist, _title);
                    break;
                case Category.JP:
                    content = await new JlyricParser().ParseHtml(_artist, _title);
                    if (content == null) content = await new JlyricParser().ParseHtml(_artist.RemoveDiacritics(), _title.RemoveDiacritics(), "-d");
                    break;
                case Category.Western:
                    content = await new MusixmatchParser().ParseHtml(_artist, _title);
                    if (content == null) content = await new MetrolyricsParser().ParseHtml(_artist, _title);
                    if (content == null) content = await new LyricsfreakParser().ParseHtml(_artist, _title);
                    break;
                case Category.Other:
                    content = await new AtwikiParser().ParseHtml(_artist, _title);
                    break;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                isAddingLyrics = false;
                return (false, null);
            }

            await SaveLyrics(artist, title, content);
            isAddingLyrics = false;

            return (true, new Lyrics()
            {
                Artist = artist,
                Title = title,
                Content = content
            });
        }

        private async Task SaveLyrics(string artist, string title, string content)
        {
            await App.Database.SaveLyricsAsync(new Lyrics()
            {
                Artist = artist.ToLower().Trim(),
                Title = title.ToLower().Trim(),
                Content = content
            });
        }

        private async Task OverwriteLyrics()
        {
            await SaveLyrics(currentSong.Artist, currentSong.Title, OriginalLyrics);
            SetStatus(Status.SaveSuccessFul);
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
                case Status.SaveSuccessFul:
                    StatusText = LocaleResources.SaveSuccessful;
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

        private void ToggleInfoRight()
        {
            ShowInfoRight(InfoRightVisibility == Visibility.Collapsed);
        }

        private void ShowInfoRight(bool flag)
        {
            if (flag)
            {
                InfoRightVisibility = Visibility.Visible;
                ShowHideInfoRightText = "⏵";
            }
            else
            {
                InfoRightVisibility = Visibility.Collapsed;
                ShowHideInfoRightText = "⏴";
            }
        }

        // Change zoom on mouse scroll
        private void OnViewMouseWheel(MouseWheelEventArgs e)
        {
            if (keysDown.Contains(Key.LeftCtrl))
            {
                e.Handled = true;
                int d = e.Delta > 0 ? 1 : -1;
                ZoomSelectionIndex += ZoomSelectionIndex + d > zoomLevels || ZoomSelectionIndex + d < 0 ? 0: d;
            }
        }

        private void ZoomSelectionChanged(SelectionChangedEventArgs e)
        {
            SetFontSize();
        }

        private void SetFontSize()
        {
            LyricsFontSize = defFontSize / 100 * (ZoomSelectionIndex * zoomStep + zoomStep);
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
        private async void OnAutoSearchChecked()
        {
            autoSearch = true;
            Properties.Settings.Default.AutoSearch = true;
            Properties.Settings.Default.Save();
            await GetCurrentlyPlaying();
        }

        private void OnAutoSearchUnchecked()
        {
            GetLyricsEnabled = true;
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
            Properties.Settings.Default.ZoomIndex = ZoomSelectionIndex;
            Properties.Settings.Default.AutoSearch = AutoSearchChecked;
            Properties.Settings.Default.ShowInfoRight = InfoRightVisibility == Visibility.Visible;

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
    }
}
