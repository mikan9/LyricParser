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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Data.SQLite;
using System.Windows.Media.Animation;
using HtmlAgilityPack;
using System.Threading;
using System.Windows.Markup;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.ComponentModel;

namespace LyricParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int MAX_RETRIES = 6;
        private static int retries = 6;
        private static bool paused = false;
        private readonly double heightDiff = 100 + 23;

        private double zoomValue = 100.0;
        private readonly double defFontSize = 12.0;
        private readonly double zoomStep = 5.0;
        private DispatcherTimer zoomTimer;
        int zoomMode = 0;

        private bool? autoSearch = true;
        private bool initComplete = false;

        WebResponse response = null;
        private List<string> rows = new List<string>();
        private List<string> english = new List<string>();
        private List<string> romaji = new List<string>();
        private List<string> original = new List<string>();
        private DispatcherTimer timer = new DispatcherTimer();
        Thread lyricsThread;

        public Status currentStatus = Status.Standby;
        public Song currentSong;
        public string currentUrl = "";
        public static Player currentPlayer = Player.Winamp;
        public static Category debug_mode = Category.None;
        static Category cat = Category.Western;
        
        int anime_retry = 0;
        int western_retry = 0;

        private ViewModel viewModel = new ViewModel();
        private List<Key> keysDown = new List<Key>();

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

            if (!Properties.UserSettings.Default.SearchAnime) AnimeRad.Visibility = Visibility.Collapsed;
            else AnimeRad.Visibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchTouhou) TouhouRad.Visibility = Visibility.Collapsed;
            else TouhouRad.Visibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchWest) WestRad.Visibility = Visibility.Collapsed;
            else WestRad.Visibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchJP) JpRad.Visibility = Visibility.Collapsed;
            else JpRad.Visibility = Visibility.Visible;

            if (!Properties.UserSettings.Default.SearchOther) OtherRad.Visibility = Visibility.Collapsed;
            else OtherRad.Visibility = Visibility.Visible;

            if (Properties.UserSettings.Default.DebugMode) debug_mode = (Category)Properties.UserSettings.Default.DebugCategory;
            else debug_mode = Category.None;

            if (debug_mode == Category.None) cat = (Category)Properties.Settings.Default.LastCategory;

        }

        // Load themes into dictionaries
        public void LoadTheme()
        {
            Uri resUri = new Uri("/Resources.xaml", UriKind.Relative);
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


        public MainWindow()
        {
            DataContext = viewModel;
            InitializeComponent();

            Top = Properties.Settings.Default.Top >= 0 ? Properties.Settings.Default.Top : 0;
            Left = Properties.Settings.Default.Left >= 0 ? Properties.Settings.Default.Left : 0;
            Height = Properties.Settings.Default.Height;
            Width = Properties.Settings.Default.Width;
            if (Properties.Settings.Default.Maximized) WindowState = WindowState.Maximized;

            HistoryEntry LastSong = DatabaseHandler.GetLastSong();

            SongNameTxt.SelectedItem = LastSong;
       
            LoadTheme();
            currentSong = Song.Empty();
   
            var themes = Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory + @"Themes\", "*", SearchOption.TopDirectoryOnly)
                                  .Select(System.IO.Path.GetFileNameWithoutExtension);

            Properties.UserSettings.Default.Themes.Clear();
            Properties.UserSettings.Default.Themes.AddRange(themes.ToArray());
            Properties.UserSettings.Default.Save();

            LoadSettings();

            currentPlayer = (Player)Properties.Settings.Default.LastPlayer;

            switch (cat)
            {
                case Category.Anime:
                    AnimeRad.IsChecked = true;
                    break;
                case Category.Touhou:
                    TouhouRad.IsChecked = true;
                    break;
                case Category.Western:
                    WestRad.IsChecked = true;
                    break;
                case Category.JP:
                    JpRad.IsChecked = true;
                    break;
                case Category.Other:
                    OtherRad.IsChecked = true;
                    break;
            }

            zoomTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100),
                IsEnabled = false
            };
            zoomTimer.Tick += ZoomTimer_Tick;

            SetStatus(Status.Standby);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60);
            timer.Tick += Timer_Tick;
            timer.Start();
            initComplete = true;
        }

        // Send the editied lyrics to the database to be processed
        public bool EditLyrics(string title, string title_en, string artist, string original, string romaji, string english, Category genre)
        {
            List<string> lyrics = new List<string>
            {
                original,
                romaji,
                english
            };

            bool success = DatabaseHandler.AddSong(artist, title, title_en, genre, lyrics, DatabaseHandler.LyricsExist(currentSong, false, this));

            if(currentSong.Artist == artist && currentSong.Title == title)
                SetLyrics(original, romaji, english);

            return success;
        }

        // Show the edited lyrics in the main window
        public void SetLyrics(string orig = "", string rom = "", string eng = "")
        {
            original.Clear();
            romaji.Clear();
            english.Clear();

            original.Add(orig);
            romaji.Add(rom);
            english.Add(eng);
            SetUpTables(true);
        }

        // Timer to check for song info changes
        private void Timer_Tick(object sender, EventArgs e)
        {
            autoSearch = autoSearchBox.IsChecked;
            if (autoSearch == true && Song.GetSongInfo().Title != "Winamp 5.666 Build 3516")
            {
                if ((Song.GetSongInfo().Title != currentSong.Title && !paused))
                {
                    currentSong = Song.GetSongInfo();
                    if (!DatabaseHandler.LyricsExist(currentSong, true, this))
                    {
                        retries = MAX_RETRIES;
                        GetLyricsHTML(Song.GetSongInfo().Title);
                    }
                }
            }
        }

        // Add a new HistoryEntry to the collection, remove last item if count exeeds 20 and remove duplicate if exists
        private void AddHistoryEntry(string data)
        {
            if (data == " - ") return;
            if (viewModel.SearchHistory != null && viewModel.SearchHistory.Count > 0)
            {
                if (viewModel.SearchHistory.Count == 20)
                {
                    viewModel.SearchHistory.RemoveAt(19);
                }
                if (viewModel.SearchHistory.Any(s => s.Data == data))
                    viewModel.SearchHistory.Remove(viewModel.SearchHistory.Single(s => s.Data == data));
            }

            viewModel.AddHistoryEntry(new HistoryEntry { Data = data });
            SongNameTxt.SelectedItem = viewModel.SearchHistory.ElementAt(0);
        }
        
        // Update last song and search history then begin fetching lyrics for the current song
        private void GetLyricsBtn_Click(object sender, RoutedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(SongNameTxt.Text) && autoSearchBox.IsChecked == false)
            {

                string data;
                if (viewModel.SearchHistory != null && viewModel.SearchHistory.Count > 0)
                    data = SongNameTxt.Text;
                else
                    data = new HistoryEntry { Data = SongNameTxt.Text }.Data;

                AddHistoryEntry(data);

                int index = data.Trim().IndexOf(" - ");
                currentSong.Artist = data.Trim().Substring(0, index);
                currentSong.Title = data.Trim().Substring(index + 3);

                DatabaseHandler.UpdateLastSong(currentSong.Artist, currentSong.Title);
                retries = MAX_RETRIES;

                GetLyricsHTML(currentSong.Title);
            }
            else if(autoSearchBox.IsChecked == true)
            {
                Trace.WriteLine("Force Searching");
                retries = MAX_RETRIES;
                currentSong = Song.GetSongInfo();
                GetLyricsHTML(Song.GetSongInfo().Title);
            }
        }

        // Begin fetching lyrics on a separate thread
        private void GetLyricsHTML(string name)
        {
            SetStatus(Status.Searching);
            if (currentSong.Title == "Not running" || currentSong.Artist == null) return;
            AnimeRad.Checked -= AnimeRad_Checked;
            TouhouRad.Checked -= TouhouRad_Checked;
            JpRad.Checked -= JpRad_Checked;
            WestRad.Checked -= WestRad_Checked;

            Trace.WriteLine("Category: " + cat);
            Trace.WriteLine("Beginning search of lyrics for Artist: " + currentSong);
            CleanUp();
            name = currentSong.Title;

            SongInfoTxt.Text = currentSong.Artist + " - " + currentSong.Title;
            anime_retry = 0;

            if (Properties.UserSettings.Default.SearchAnime)
            {
                AnimeRad.Checked += AnimeRad_Checked;
                cat = Category.Anime;
            }
            if (Properties.UserSettings.Default.SearchTouhou)
            {
                TouhouRad.Checked += TouhouRad_Checked;
                cat = Category.Touhou;
            }
            if (Properties.UserSettings.Default.SearchWest)
            {
                WestRad.Checked += WestRad_Checked;
                cat = Category.Western;
            }
            if (Properties.UserSettings.Default.SearchJP)
            {
                JpRad.Checked += JpRad_Checked;
                cat = Category.JP;
            }
            if (Properties.UserSettings.Default.SearchOther)
            {
                OtherRad.Checked += OtherRad_Checked;
                cat = Category.Other;
            }



            if (lyricsThread == null || lyricsThread.ThreadState == System.Threading.ThreadState.Stopped)
            {
                lyricsThread = new Thread(() => GetLyrics(cat));
                lyricsThread.Start();
            }
        }

        // Fetch the correct url for the song based on category
        private void GetLyrics(Category category)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            anime_retry = 0;
            western_retry = 0;
            switch (category)
            {
                case Category.Anime:
                    GetAnimeLyricUrl(currentSong.Title);
                    break;
                case Category.Touhou:
                    GetTouhouLyricUrl(currentSong.Title);
                    break;
                case Category.Western:
                case Category.Other:
                    GetWesternLyricUrl(currentSong.Title);
                    break;
                case Category.JP:
                    GetJPLyricUrl(currentSong.Title);
                    break;
            }
            return;
        }

        // Retry fetching lyrics if none were found
        private void RetryGettingLyrics(Category category, string name)
        {

            if (retries > 0)
            {
                SetStatus(Status.Searching);
                cat = category;
                Interlocked.Decrement(ref retries);

                switch (category)
                {
                    case Category.Anime:
                        if (Properties.UserSettings.Default.SearchTouhou) GetTouhouLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchWest) GetWesternLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchJP) GetJPLyricUrl(name);
                        else GetAnimeLyricUrl(name);
                        break;
                    case Category.Touhou:
                        if (Properties.UserSettings.Default.SearchWest) GetWesternLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchJP) GetJPLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchAnime) GetAnimeLyricUrl(name);
                        else GetTouhouLyricUrl(name);
                        break;
                    case Category.Western:
                        if (Properties.UserSettings.Default.SearchJP) GetJPLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchAnime) GetAnimeLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchTouhou) GetTouhouLyricUrl(name);
                        else GetWesternLyricUrl(name);
                        break;
                    case Category.JP:
                        if (Properties.UserSettings.Default.SearchTouhou) GetTouhouLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchWest) GetWesternLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchOther) GetWesternLyricUrl(name);
                        else GetJPLyricUrl(name);
                        break;
                    case Category.Other:
                        if (Properties.UserSettings.Default.SearchAnime) GetAnimeLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchTouhou) GetTouhouLyricUrl(name);
                        else if (Properties.UserSettings.Default.SearchAnime) GetAnimeLyricUrl(name);
                        else GetWesternLyricUrl(name);
                        break;
                }
            }
            else
            {
                SetStatus(Status.Failed);
                return;
            }
        }

        // Begin parsing fetched lyrics
        private void ParseLyrics(LyricsDatabase src, WebResponse resp)
        {
            SetStatus(Status.Parsing);

            switch (src)
            {
                case LyricsDatabase.Gendou:
                    ParseAnimeLyrics(resp);
                    break;
                case LyricsDatabase.Touhouwiki:
                    ParseTouhouLyrics(resp);
                    break;
                case LyricsDatabase.Musicxmatch:
                    ParseWestern(resp);
                    break;
                case LyricsDatabase.JLyric:
                    ParseJLyric(resp);
                    break;
                case LyricsDatabase.Utanet:
                    ParseUtanet(resp);
                    break;
                case LyricsDatabase.Atwiki:
                    ParseAtwiki(resp);
                    break;
            }
        }

        // Fetch anime lyrics
        private void GetAnimeLyricUrl(string name)
        {
            if (name == null) return;
            Application.Current.Dispatcher.Invoke(new Action(() => { AnimeRad.IsChecked = true; }));
            CleanUp();

            string url = "";
            string _url = GetURL(currentSong.Artist, name, LyricsDatabase.Gendou);
            if (anime_retry > 0)
            {
                _url = GetURL(currentSong.Artist, name, LyricsDatabase.Gendou, "&page=" + anime_retry);
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "GET";
            response = request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(reader.ReadToEnd());
                var table = doc.DocumentNode.SelectSingleNode("//table");
                try
                {
                    var tr = table.Elements("tr");
                    var children = tr.ElementAt(2).ChildNodes;

                    // 1 = Title      5 = Artist       Url = 13

                    for (int j = 2; j < tr.ToArray().Length; ++j)
                    {
                        children = tr.ElementAt(j).ChildNodes;

                        string _title = children.ElementAt(1).FirstChild.InnerText.ToLower().TrimStart().TrimEnd();
                        string _artist = children.ElementAt(5).ChildNodes.ElementAt(1).InnerText.ToLower().TrimStart().TrimEnd();

                        if (_title == name.ToLower() && _artist == currentSong.Artist.ToLower())
                        {
                            anime_retry = 0;
                            url = "http://gendou.com" + children.ElementAt(13).ChildNodes.ElementAt(1).GetAttributeValue("href", "null").Split('&')[0];
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                            req.Method = "GET";
                            response = req.GetResponse();
                            ParseLyrics(LyricsDatabase.Gendou, response);
                            break;
                        }
                    }
                }
                catch {
                    if (anime_retry < -1)
                    {
                        anime_retry++;
                        GetAnimeLyricUrl(name);
                    }
                    else if (autoSearch == true)
                    {
                        RetryGettingLyrics(Category.Anime, name);
                    }
                    return;
                }
            }
            return;
        }

        // Fetch touhou lyrics
        private void GetTouhouLyricUrl(string name)
        {
            Trace.WriteLine("Searching for Touhou Lyrics with Title: " + name);
            if (name == null) return;
            Application.Current.Dispatcher.Invoke(new Action(() => { TouhouRad.IsChecked = true; }));

            string url = "";
            string _url = GetURL(currentSong.Artist, name, LyricsDatabase.Touhouwiki);

            url = GetURL(currentSong.Artist, name, LyricsDatabase.Touhouwiki);
            if (url != "")
            {     
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.KeepAlive = false;
                req.ProtocolVersion = HttpVersion.Version10;
                req.ServicePoint.ConnectionLimit = 1;
                if ((int)((HttpWebResponse)req.GetResponseWithoutException()).StatusCode == 200)
                {
                    response = req.GetResponse();
                    ParseLyrics(LyricsDatabase.Touhouwiki, response);
                }
                else
                {
                    if (autoSearch == true)
                    {
                        RetryGettingLyrics(Category.Touhou, currentSong.Title);
                    }
                }
            }
            else
            {
                if (autoSearch == true)
                {
                    RetryGettingLyrics(Category.Touhou, currentSong.Title);
                }
            }
        }

        // Fetch western lyrics
        private void GetWesternLyricUrl(string name = "")
        {
            Trace.WriteLine("Searching for Western Lyrics with Title: " + name + " Artist: " + currentSong.Artist);
            if (currentSong.Title == null) return;
            Application.Current.Dispatcher.Invoke(new Action(() => { WestRad.IsChecked = true; }));

            if (name == "") name = currentSong.Title;
            try
            {
                string artist = currentSong.Artist;
                int index = artist.IndexOf('"');
                if (index >= 0)
                {
                    artist = artist.Remove(index, 1).Insert(index, "“");
                }
                index = artist.IndexOf('"');
                if (index >= 0)
                {
                    artist = artist.Remove(index, 1).Insert(index, "”");
                }

                string url = "";
                string _url = GetURL(artist, name, LyricsDatabase.Musicxmatch);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
                request.Method = "GET";
                response = request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(reader.ReadToEnd());
                    var cards = doc.DocumentNode.SelectNodes("//*[contains(@class,'media-card-text')]");
                    try
                    {
                        for (var i = 0; i < cards.Count; ++i)
                        {
                            var child = cards.ElementAt(i);
                            var titleWrapper = child.SelectSingleNode(".//*[contains(@class, 'title')]");
                            var artistWrapper = child.SelectSingleNode(".//*[contains(@class, 'artist')]");

                            string _title = titleWrapper.FirstChild.InnerText.ToLower().TrimStart().TrimEnd();
                            string _artist = artistWrapper.InnerText.ToLower().TrimStart().TrimEnd();

                            List<string> artists = new List<string>();
                            artists.AddRange(currentSong.Artist.Split(new string[] { "feat.", ",", "&", ".ft", "ft.", " x " }, StringSplitOptions.RemoveEmptyEntries));
                            for(int j = 0; j < artists.Count; ++j)
                            {
                                artists[j] = artists[j].ToLower().TrimStart().TrimEnd();
                            }

                            if (_title.RemoveBracket('(').Trim().Replace('’', '\'').Contains(currentSong.Title.ToLower()) && artists.Any(a => _artist.Contains(a.ToLower())))
                            {
                                url = "https://www.musixmatch.com" + titleWrapper.InnerHtml.Split(new string[] { "href=\"" }, StringSplitOptions.None)[1].Split('"')[0];
                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                                req.Method = "GET";
                                response = req.GetResponse();
                                ParseLyrics(LyricsDatabase.Musicxmatch, response);
                                break;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Trace.WriteLine(e.ToString());
                        if (autoSearch == true)
                        {
                            Trace.WriteLine("Failed searching for western lyrics...");
                            RetryGettingLyrics(Category.Western, name);
                        }
                        return;
                    }
                }
                return;

            }
            catch(Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (lyricsThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    if (western_retry < 2)
                    {
                        western_retry++;
                        int index = name.Trim().IndexOf(" - ");
                        if (index >= 0)
                        {
                            name = name.Trim().Substring(0, index);
                        }
                        index = name.Trim().IndexOf(" (");
                        if (index >= 0)
                        {
                            name = name.Trim().Substring(0, index);
                        }
                        index = name.Trim().IndexOf(" [");
                        if (index >= 0)
                        {
                            name = name.Trim().Substring(0, index);
                        }
                        GetWesternLyricUrl(name);
                    }
                    else
                    {
                        if (autoSearch == true)
                        {
                            RetryGettingLyrics(Category.Western, currentSong.Title);
                        }
                    }
                }
            }
        }

        // Fetch JP lyrics using JLyric's search engine
        private void SearchJLyric()
        {
            if (currentSong.Title == null) return;
            Application.Current.Dispatcher.Invoke(new Action(() => { JpRad.IsChecked = true; }));
            string name = currentSong.Title;
            string url = "";
            string _url = GetURL(currentSong.Artist, name, LyricsDatabase.JLyric);
            bool foundMatch = false;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "GET";
            try
            {
                response = request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(reader.ReadToEnd());

                    var mnb = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'mnb')]");
                    var bdy = mnb.SelectNodes(".//*[contains(@class, 'bdy')]");

                    for (int i = 0; i < bdy.ToArray().Length; ++i)
                    {
                        string _title = "";
                        string _artist = "";

                        try
                        {
                            var mid = bdy.ElementAt(i).SelectSingleNode(".//*[contains(@class,'mid')]");
                            var sml = bdy.ElementAt(i).SelectSingleNode(".//*[contains(@class,'sml')]");

                            _title = mid.FirstChild.InnerText.ToLower().TrimStart().TrimEnd();
                            _artist = sml.ChildNodes.ElementAt(1).InnerText.ToLower().TrimStart().TrimEnd();

                            if (_title == name.ToLower() && _artist.Contains(currentSong.Artist.ToLower()))
                            {
                                foundMatch = true;
                                url = mid.FirstChild.GetAttributeValue("href", "null");

                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                                req.Method = "GET";
                                response = req.GetResponse();
                                ParseLyrics(LyricsDatabase.JLyric, response);
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine(e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }

            if (!foundMatch)
            {
                SearchAtwiki();
                // SearchUtanet(); Usage is blocked in EU due to GDPR. Disabled for now.
                //if (autoSearch == true)
                //{
                //    RetryGettingLyrics(Category.JP, name);
                //}
            }
        }

        // Fetch JP lyrics using Utanet's search engine
        private void SearchUtanet()
        {
            string name = currentSong.Title;
            string url;
            string _url = GetURL(currentSong.Artist, name, LyricsDatabase.Utanet);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "GET";
            try
            {
                response = request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(reader.ReadToEnd());
                    try
                    {
                        var table = doc.DocumentNode.SelectSingleNode("//tbody");
                        var tr = table.Elements("tr");
                        bool foundMatch = false;
                        for (int i = 0; i < tr.ToArray().Length; ++i)
                        {
                            string _title = tr.ElementAt(i).ChildNodes.ElementAt(0).FirstChild.InnerText.ToLower().TrimStart().TrimEnd();
                            string _artist = tr.ElementAt(i).ChildNodes.ElementAt(1).InnerText.ToLower().TrimStart().TrimEnd();

                            if (_title == name.ToLower() && _artist.Contains(currentSong.Artist.ToLower()))
                            {
                                foundMatch = true;
                                url = "https://www.uta-net.com" + tr.ElementAt(i).ChildNodes.ElementAt(0).FirstChild.GetAttributeValue("href", "null");
                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                                req.Method = "GET";
                                response = req.GetResponse();
                                ParseLyrics(LyricsDatabase.Utanet, response);
                                break;
                            }
                        }
                        if (!foundMatch)
                        {
                            if (autoSearch == true)
                            {
                                RetryGettingLyrics(Category.JP, name);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (autoSearch == true)
                {
                    RetryGettingLyrics(Category.JP, name);
                }
            }
        }

        // Search vocaloid lyrics using Atwiki's search engine
        private void SearchAtwiki()
        {
            string name = currentSong.Title.Replace("-", "");
            string url;
            string _url = GetURL(currentSong.Artist, name, LyricsDatabase.Atwiki);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.Method = "GET";
            try
            {
                response = request.GetResponse();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(reader.ReadToEnd());
                    try
                    {
                        bool foundMatch = false;
                        var body = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'wikibody')]");
                        var ul = body.SelectSingleNode("ul");
                        foreach (var child in ul.SelectNodes("li"))
                        {
                            var a = child.SelectSingleNode("a");
                            if(a.InnerText.ToLower().TrimStart().TrimEnd() == name.ToLower().TrimStart().TrimEnd())
                            {
                                foundMatch = true;
                                url = "https:" + a.GetAttributeValue("href", "null").Replace(" ", "%20");

                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                                req.Method = "GET";
                                response = req.GetResponse();
                                ParseLyrics(LyricsDatabase.Atwiki, response);
                                break;
                            }
                        }

                        if (!foundMatch)
                        {
                            if (autoSearch == true)
                            {
                                RetryGettingLyrics(Category.JP, name);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                if (autoSearch == true)
                {
                    RetryGettingLyrics(Category.JP, name);
                }
            }
        }

        private void GetJPLyricUrl(string name)
        {
            CleanUp();

            SearchJLyric();
        }

        // Parse anime lyrics from fetched web response
        private void ParseAnimeLyrics(WebResponse resp)
        {
            if (resp == null)
            {
                return;
            }
            try
            {
                currentSong.Genre = cat;
                DatabaseHandler.GetEnglishTitle(currentSong);

                anime_retry = 0;

                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string _original = "";
                    string _romaji = "";
                    string _english = "";

                    SetCurrentSong(currentSong.Artist + " - " + currentSong.Title);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());
                    _original = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'content_1')]").InnerHtml.Replace("<br>", "");
                    _romaji = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'content_0')]").InnerHtml.Replace("<br>", "");
                    _english = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'content_2')]").InnerHtml.Replace("<br>", "");


                    romaji.Add(_romaji);
                    original.Add(_original);
                    english.Add(_english);
                    SetUpTables(true);
                }

            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        // Parse touhou lyrics from fetched web response
        private void ParseTouhouLyrics(WebResponse resp)
        {
            if (resp == null)
            {
                return;
            }
            try
            {
                Trace.WriteLine("Parsing touhou with url: " + resp.ResponseUri);
                currentSong.Genre = cat;

                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    List<string> _kanji = new List<string>();
                    List<string> _romaji = new List<string>();
                    List<string> _english = new List<string>();

                    SetCurrentSong(currentSong.Artist + " - " + currentSong.Title);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());

                    if (doc.DocumentNode.SelectSingleNode("//*[contains(@class,'noarticletext')]") == null)
                    {
                        var tr = doc.DocumentNode.SelectNodes("//*[contains(@class,'lyrics_row')]");
                        var children = tr;

                        for (int j = 0; j < tr.ToArray().Length; ++j)
                        {
                            children = tr.ElementAt(j).ChildNodes;
                            if (children.Count == 4 || children.Count == 3)
                            {
                                _kanji.Add(children.ElementAt(0).InnerText);
                                _romaji.Add(children.ElementAt(1).InnerText);
                            }
                            if (children.Count == 4 || children.Count == 2)
                            {
                                int id = 2;
                                if (children.Count == 2) id = 0;
                                _english.Add(children.ElementAt(id).InnerText);
                            }
                        }

                        foreach (string s in _kanji)
                        {
                            original.Add(s);
                        }
                        foreach (string s in _romaji)
                        {
                            romaji.Add(s);
                        }
                        foreach (string s in _english)
                        {
                            english.Add(s);
                        }
                        SetUpTables(true);
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        // Parse western lyrics from fetched web response
        private void ParseWestern(WebResponse resp)
        {
            try
            {
                currentSong.Genre = cat;

                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    SetCurrentSong(currentSong.Artist + " - " + currentSong.Title);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());
                    var _empty = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'mxm-lyrics-not-available')]");
                    if (_empty != null)
                    {
                        if (autoSearch == true)
                        {
                            RetryGettingLyrics(Category.JP, currentSong.Title);
                        }
                        else
                        {
                            SetStatus(Status.Failed);
                        }
                    }

                    var _english = doc.DocumentNode.SelectNodes("//*[contains(@class,'mxm-lyrics__content ')]");

                    foreach (var n in _english)
                    {
                        english.Add(n.Element("span").InnerHtml);
                        english.Add("\r\n");
                    }
                    english.Add("\r\n");

                    SetUpTables(true);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        // Parse JP lyrics from fetched web response from JLyrics
        private void ParseJLyric(WebResponse resp)
        {
            try
            {
                currentSong.Genre = cat;

                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string _kanji = "";
                    SetCurrentSong(currentSong.Artist + " - " + currentSong.Title);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());
                    _kanji = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'Lyric')]").InnerHtml.Replace("<br>", "\r\n") + "\r\n";

                    original.Add(_kanji);
                    SetUpTables(true);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        // Parse JP lyrics from fetched web response from Utanet
        private void ParseUtanet(WebResponse resp)
        {
            try
            {
                currentSong.Genre = cat;

                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string _kanji = "";
                    SetCurrentSong(currentSong.Artist + " - " + currentSong.Title);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());
                    _kanji = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'kashi_area')]").InnerHtml.Replace("<br>", "\r\n") + "\r\n";

                    original.Add(_kanji);
                    SetUpTables(true);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        // Parse vocaloid lyrics from fetched web response
        private void ParseAtwiki(WebResponse resp)
        {
            try
            {
                currentSong.Genre = cat;

                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string _kanji = "";
                    SetCurrentSong(currentSong.Artist + " - " + currentSong.Title);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(sr.ReadToEnd());
                    var body = doc.DocumentNode.SelectSingleNode("//*[contains(@id,'wikibody')]");

                    bool foundLyrics = false;

                    foreach(var child in body.ChildNodes)
                    {
                        if (!foundLyrics && child.InnerText != "歌詞")
                            continue;
                        else if (child.InnerText == "歌詞" && !foundLyrics)
                        {
                            foundLyrics = true;
                            continue;
                        }

                        if (foundLyrics)
                        {
                            if (child.InnerText == "コメント") break;

                            if (child.HasChildNodes)
                            {
                                if (_kanji.Length > 0) _kanji += "\r\n\r\n";
                                _kanji += child.InnerHtml.Replace("\n", "").Replace("<br>", "\n");
                            }
                        }

                    }

                    original.Add(_kanji);
                    SetUpTables(true);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        private void ParseJPLyrics(WebResponse resp)
        {
            if (resp == null)
            {
                return;
            }
            ParseJLyric(resp);
        }

        // Update window title to the current song
        private void SetCurrentSong(string value)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                SongInfoTxt.Text = value;
                this.Title = value;
            }
            ));
        }

        // Get the corresponding URL based on the category
        private string GetURL(string artist, string title, LyricsDatabase database, string optional = "")
        {
            string url = "";

            switch (database)
            {
                case LyricsDatabase.Gendou:
                    url = "http://gendou.com/amusic/?filter=" + artist.Replace(" ", "+") + "+" + title.Replace(" ", "+") + optional;
                    break;
                case LyricsDatabase.Touhouwiki:
                    url = "https://en.touhouwiki.net/wiki/Lyrics:_" + title.Replace(" ", "_").Replace("[", "(").Replace("]", ")");
                    break;
                case LyricsDatabase.Musicxmatch:
                    url = "https://www.musixmatch.com/search/" + artist.Replace(" ", "%20") + "-" + title.Replace(" ", "%20");
                    break;
                case LyricsDatabase.JLyric:
                    url = "http://search.j-lyric.net/index.php?kt=" + title.Replace(" ", "+") + "&ct=1&ka=" + artist;
                    break;
                case LyricsDatabase.Utanet:
                    url = "https://www.uta-net.com/search/?Aselect=2&Keyword=" + title.Replace(" ", "+");
                    break;
                case LyricsDatabase.Atwiki:
                    url = "https://www5.atwiki.jp/hmiku/?cmd=wikisearch&keyword=" + title.Replace(" ", "+");
                    break;
            }
            currentUrl = url;

            return url;
        }

        // Cleanup lyrics view
        private void CleanUp()
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                rows.Clear();

                OriginalTxt.Clear();
                RomajiTxt.Clear();
                EnglishTxt.Clear();

                original.Clear();
                romaji.Clear();
                english.Clear();
            }));

        }

        // Set status of the search
        public void SetStatus(Status status)
        { 
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                currentStatus = status;
                switch (status)
                {
                    case Status.Done:
                        StatusTxt.Text = LocaleResources.FoundLyrics;
                        break;
                    case Status.Searching:
                        StatusTxt.Text = LocaleResources.Searching;
                        break;
                    case Status.Parsing:
                        StatusTxt.Text = LocaleResources.Parsing;
                        break;
                    case Status.Failed:
                        StatusTxt.Text = LocaleResources.Failed;
                        break;
                    case Status.Standby:
                        StatusTxt.Text = LocaleResources.Standby;
                        break;
                }
            }));
        }

        // Set category
        private void SetCategory(Category id)
        {
            if (!initComplete) return;
            cat = id;
            GetLyricsHTML(Song.GetSongInfo().Title);
        }

        // Setup grid that will contain the lyrics
        private void SetUpTables(bool cleanUp)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                SetStatus(Status.Done);

                OriginalTxt.Text = "";
                RomajiTxt.Text = "";
                EnglishTxt.Text = "";

                OriginalTxt.Visibility = System.Windows.Visibility.Collapsed;
                OriginalLbl.Visibility = OriginalTxt.Visibility;
                RomajiTxt.Visibility = System.Windows.Visibility.Collapsed;
                RomajiLbl.Visibility = RomajiTxt.Visibility;
                EnglishTxt.Visibility = System.Windows.Visibility.Collapsed;
                EnglishLbl.Visibility = EnglishTxt.Visibility;

                if (english.Count > 0 && original.Count == 0)
                {
                    original.AddRange(english);
                    english.Clear();
                }

                ContentGrid.ColumnDefinitions.Clear();
                HeaderGrid.ColumnDefinitions.Clear();

                bool bShowOrig = false;
                bool bShowRom = false;
                bool bShowEng = false;

                switch (cat)
                {
                    case Category.Anime:
                        bShowOrig = Properties.UserSettings.Default.OrigAnime;
                        bShowRom = Properties.UserSettings.Default.RomajiAnime;
                        bShowEng = Properties.UserSettings.Default.EngAnime;
                        break;
                    case Category.Touhou:
                        bShowOrig = Properties.UserSettings.Default.OrigTouhou;
                        bShowRom = Properties.UserSettings.Default.RomajiTouhou;
                        bShowEng = Properties.UserSettings.Default.EngTouhou;
                        break;
                    case Category.Western:
                        bShowOrig = Properties.UserSettings.Default.OrigWest;
                        bShowRom = Properties.UserSettings.Default.RomajiWest;
                        bShowEng = Properties.UserSettings.Default.EngWest;
                        break;
                    case Category.JP:
                        bShowOrig = Properties.UserSettings.Default.OrigJP;
                        bShowRom = Properties.UserSettings.Default.RomajiJP;
                        bShowEng = Properties.UserSettings.Default.EngJP;
                        break;
                    case Category.Other:
                        bShowOrig = Properties.UserSettings.Default.OrigOther;
                        bShowRom = Properties.UserSettings.Default.RomajiOther;
                        bShowEng = Properties.UserSettings.Default.EngOther;
                        break;
                }

                if (bShowOrig && original.Count > 0)
                {
                    ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

                    Grid.SetColumn(OriginalTxt, 0);
                    Grid.SetColumn(OriginalLbl, 0);

                    OriginalTxt.Visibility = System.Windows.Visibility.Visible;
                    OriginalLbl.Visibility = OriginalTxt.Visibility;

                    if (bShowRom && romaji.Count > 0)
                    {
                        ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

                        Grid.SetColumn(RomajiLbl, 1);
                        Grid.SetColumn(RomajiTxt, 1);

                        RomajiTxt.Visibility = System.Windows.Visibility.Visible;
                        RomajiLbl.Visibility = RomajiTxt.Visibility;
                    }
                    if (bShowEng && english.Count > 0)
                    {
                        ContentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

                        int col = 2;
                        if (!bShowRom) col = 1;

                        Grid.SetColumn(EnglishLbl, col);
                        Grid.SetColumn(EnglishTxt, col);

                        EnglishTxt.Visibility = System.Windows.Visibility.Visible;
                        EnglishLbl.Visibility = EnglishTxt.Visibility;
                    }
                }

                if (cleanUp)
                {
                    CleanUpLyrics();
                }
            }));
            return;
        }

        // Cleanup fetched lyrics
        private void CleanUpLyrics()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (original.Count > 0)
                {
                    foreach (string s in original)
                    {
                        OriginalTxt.Text += HtmlEntity.DeEntitize(s);
                    }
                }
                if (romaji.Count > 0)
                {
                    foreach (string s in romaji)
                    {
                        RomajiTxt.Text += HtmlEntity.DeEntitize(s);
                    }
                }
                if (english.Count > 0)
                {
                    foreach (string s in english)
                    {
                        EnglishTxt.Text += HtmlEntity.DeEntitize(s);
                    }
                }
            }));
        }

        // Change zoom level ie. change font size
        private void Zoom(double d)
        {
            zoomValue = d > 5 ? d : 5;
            ZoomTxt.Text = zoomValue.ToString() + " %";

            double newSize = defFontSize / 100 * zoomValue;

            RomajiTxt.FontSize = newSize;
            EnglishTxt.FontSize = RomajiTxt.FontSize;
            OriginalTxt.FontSize = RomajiTxt.FontSize;
        }

        // Change zoom on mouse scroll
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (keysDown.Contains(Key.LeftCtrl))
            {
                e.Handled = true;
                double x = e.Delta > 0 ? zoomStep : -zoomStep;
                Zoom(zoomValue + x);
            }
        }

        private void EnlargeBtn_Click(object sender, RoutedEventArgs e)
        {
            Zoom(zoomValue + zoomStep);
        }
        private void ShrinkBtn_Click(object sender, RoutedEventArgs e)
        {
            Zoom(zoomValue - zoomStep);
        }

        private void AnimeRad_Checked(object sender, RoutedEventArgs e)
        {
            SetCategory(Category.Anime);
        }

        private void TouhouRad_Checked(object sender, RoutedEventArgs e)
        {
            SetCategory(Category.Touhou);
        }

        private void WestRad_Checked(object sender, RoutedEventArgs e)
        {
            SetCategory(Category.Western);
        }

        private void JpRad_Checked(object sender, RoutedEventArgs e)
        {
            SetCategory(Category.JP);
        }

        private void OtherRad_Checked(object sender, RoutedEventArgs e)
        {
            SetCategory(Category.Other);
        }

        // Update grid height relative to window height
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double newHeight = this.ActualHeight - heightDiff;
            if (newHeight > 0)
            {
                OriginalTxt.Height = newHeight;
                RomajiTxt.Height = newHeight;
                EnglishTxt.Height = newHeight;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void AutoSearchBox_Checked(object sender, RoutedEventArgs e)
        {
            SongNameTxt.IsEnabled = false;
            autoSearch = true;
            Properties.Settings.Default.AutoSearch = true;
            Properties.Settings.Default.Save();
        }

        private void AutoSearchBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GetLyricsBtn.IsEnabled = true;
            SongNameTxt.IsEnabled = true;
            autoSearch = false;
            Properties.Settings.Default.AutoSearch = false;
            Properties.Settings.Default.Save();
        }

        private void Btn_MouseEnter(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            BitmapImage bmp = new BitmapImage(new Uri("/LyricParser;component/Images/" + img.Name + "Hover.png", UriKind.Relative))
            {
                CacheOption = BitmapCacheOption.OnLoad
            };
            img.Source = bmp;
        }

        private void Btn_MouseLeave(object sender, MouseEventArgs e)
        {
            Image img = (Image)sender;
            BitmapImage bmp = new BitmapImage(new Uri("/LyricParser;component/Images/" + img.Name + ".png", UriKind.Relative))
            {
                CacheOption = BitmapCacheOption.OnLoad
            };
            img.Source = bmp;
        }

        private void EditLyricsBtn_Click(object sender, RoutedEventArgs e)
        {
            EditLyrics el = new EditLyrics(this);
            el.ShowDialog();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.LastCategory = (int)cat;
            Properties.Settings.Default.LastPlayer = PlayerBox.SelectedIndex;
            Properties.Settings.Default.ZoomLevel = zoomValue;

            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }
            Properties.Settings.Default.Save();
        }

        private void PlayerBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentPlayer = (Player)((ComboBox)sender).SelectedIndex;
        }

        private void GetCurrentSong_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            currentSong = Song.GetSongInfo();
            string artist = currentSong.Artist;
            string title = currentSong.Title;
            AddHistoryEntry(artist + " - " + title);
        }
        private void SearchInBrowser_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Process.Start("https://www.musixmatch.com/search/" + songNameTxt.Text);
            if(currentUrl != "") Process.Start(currentUrl);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow win = new SettingsWindow();
            win.Closed += Settings_Closed;
            win.ShowDialog();
        }

        private void Settings_Closed(object sender, EventArgs e)
        {
            if(((SettingsWindow)sender).newSettings) LoadSettings();  
        }

        private void ZoomTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            Zoom(double.Parse(new string(ZoomTxt.Text.TakeWhile(Char.IsDigit).ToArray())));
        }

        private void ZoomTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Zoom(double.Parse(new string(ZoomTxt.Text.TakeWhile(Char.IsDigit).ToArray())));
            }
        }

        private void ZoomTxt_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ZoomTxt.IsFocused)
            {
                bool inc = e.Delta > 0;
                double step = 10;
                if (inc) zoomValue += step;
                else if (!inc && zoomValue - step >= 0) zoomValue -= step;
                else zoomValue = 0;

                ZoomTxt.Text = zoomValue.ToString();
            }
        }

        private void ZoomTimer_Tick(object sender, EventArgs e)
        {
            if(zoomMode != 0)
            {
                Zoom(zoomValue + zoomStep * zoomMode);
            }
        }

        private void EnlargeBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            zoomMode = 1;
            zoomTimer.Start();
        }

        private void EnlargeBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            zoomMode = 0;
            zoomTimer.Stop();
            Zoom(zoomValue + zoomStep);
        }

        private void ShrinkBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            zoomMode = -1;
            zoomTimer.Start();
        }

        private void ShrinkBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            zoomMode = 0;
            zoomTimer.Stop();
            Zoom(zoomValue - zoomStep);
        }

        private void CatRad_Click(object sender, RoutedEventArgs e)
        {
            retries = MAX_RETRIES;
        }

        private void ClearSearchHistory_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            viewModel.SearchHistory.Clear();
        }
    }
}
