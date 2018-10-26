using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LyricParser
{
    public struct Song
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hwnd, string lpString, int cch);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

        private const uint WA_WM_COMMAND_PreviousTrack = 40044;
        private const uint WA_WM_COMMAND_Play = 40045;
        private const uint WA_WM_COMMAND_Pause = 40046;
        private const uint WA_WM_COMMAND_Stop = 40047;
        private const uint WA_WM_COMMAND_NextTrack = 40048;

        const string lpClassName = "Winamp v1.x";
        const string strTtlEnd = " - Winamp";

        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        private string title;

        public string Title_EN
        {
            get { return title_en; }
            set { title_en = value; }
        }
        private string title_en;

        public string Artist
        {
            get { return artist; }
            set { artist = value; }
        }
        private string artist;

        public Category Genre
        {
            get { return genre; }
            set { genre = value; }
        }
        private Category genre;

        public Song(string title, string title_en, string artist, Category genre)
        {
            this.title = title;
            this.title_en = title_en;
            this.artist = artist;
            this.genre = genre;
        }

        public static Song Empty()
        {
            Song dummy = new Song();
            dummy.Genre = Category.None;
            dummy.Artist = "Null";
            dummy.Title = "Null";
            return dummy;
        }

        public static Song GetSongInfo()
        {
            if (MainWindow.debug_mode == Category.Touhou)
            {
                Song dummy = new Song();
                dummy.Genre = Category.Touhou;
                dummy.Artist = "柿チョコ";
                dummy.Title = "CONFINED INNOCENT";
                return dummy;
            }
            else if (MainWindow.debug_mode == Category.Anime)
            {
                Song dummy = new Song();
                dummy.Genre = Category.Anime;
                dummy.Artist = "Supercell";
                dummy.Title = "Kimi no Shiranai Monogatari";
                return dummy;
            }
            else if(MainWindow.debug_mode == Category.Western)
            {
                Song dummy = new Song();
                dummy.Genre = Category.Western;
                dummy.Artist = "Paramore";
                dummy.Title = "Decode";
                return dummy;
            }
            else if (MainWindow.debug_mode == Category.JP)
            {
                Song dummy = new Song();
                dummy.Genre = Category.JP;
                dummy.Artist = "黒石ひとみ";
                dummy.Title = "Continued Story";
                return dummy;
            }

            switch (MainWindow.currentPlayer)
            {
                case Player.Winamp:
                    return GetWinampInfo();
                case Player.Spotify:
                    return GetSpotifyInfo();
                case Player.Youtube:
                    return GetYoutubeInfo();
                case Player.GooglePlayMusic:
                    return GetGooglePlayMusicInfo();
            }

            Song song = new Song();
            song.Genre = Category.Anime;
            return song;
        }

        public override string ToString()
        {
            return String.Format("Artist: {0}" + " Title: {1}", artist, title);
        }

        public static Song GetWinampInfo()
        {
            Song song = new Song();
            song.Genre = Category.Anime;

            IntPtr hwnd = FindWindow(lpClassName, null);
            if (hwnd.Equals(IntPtr.Zero))
            {
                song.Title = "Not running";
                return song;
            }

            string lpText = new string((char)0, 100);
            int intLength = GetWindowText(hwnd, lpText, lpText.Length);
            if (lpText.Contains("Winamp 5")) return song;

            if ((intLength <= 0) || (intLength > lpText.Length))
            {
                song.Title = "unknown";
                return song;
            }

            string strTitle = lpText.Substring(0, intLength).Replace(" - Winamp", "").Remove(0, lpText.IndexOf('.') + 1);

            if(strTitle != "")
            {
                song = CleanUpInfo(strTitle);
            }

            return song;
        }

        public static Song GetSpotifyInfo()
        {
            Song song = new Song();
            song.Genre = Category.Anime;

            string spotifyName = "";
            Process[] spotify = Process.GetProcessesByName("spotify");
            foreach (Process p in spotify)
            {
                if (p.MainWindowTitle.Length > 0)
                {
                    spotifyName = p.MainWindowTitle;
                }
            }
            if (spotifyName != "" && spotifyName != "spotify" && spotifyName.Contains(" - "))
            {
                song = CleanUpInfo(spotifyName);
            }

            return song;
        }
        public static Song GetYoutubeInfo()
        {
            //Trace.WriteLine("Retrieving song info from Youtube");
            Song song = new Song();
            song.Genre = Category.Anime;

            string tabName = "";
            Process[] procs = Process.GetProcessesByName("chrome");
            foreach (Process p in procs)
            {
                if (p.MainWindowTitle.Length > 0 && p.MainWindowTitle.EndsWith(" - YouTube - Google Chrome"))
                {
                    tabName = p.MainWindowTitle.Replace(" - YouTube - Google Chrome", "");
                }
            }

            if (tabName != "")
            {
                song = CleanUpInfo(tabName);
            }

            return song;
        }
        public static Song GetGooglePlayMusicInfo()
        {
            Song song = new Song();
            song.Genre = Category.Anime;

            string tabName = "";
            Process[] procs = Process.GetProcessesByName("chrome");
            foreach (Process p in procs)
            {
                if (p.MainWindowTitle.Length > 0)
                {
                    tabName = p.MainWindowTitle;
                }
            }

            if (tabName != "" && tabName.EndsWith("Google Play Music - Google Chrome"))
            {
                song = CleanUpInfo(tabName.Replace("Google Play Music - Google Chrome", ""));
            }

            return song;
        }

        private static Song CleanUpInfo(string data)
        {
            Song song = new Song();
            song.genre = Category.Anime;

            data = data.Split(new string[] { " / " }, StringSplitOptions.None)[0];
            data = data.RemoveBracket('[');
            data = data.RemoveBracket('(', new string[] { "(ft.", "(feat" });
            data = data.Replace(new string[] { "(", ")" }, "");
            data = data.Replace("*", "");

            string[] separators = new string[] { " - ", " – " };
            string[] jpSeparators = new string[] { "「", "『" };
            Dictionary<string, string> jpDict = new Dictionary<string, string>();
            jpDict.Add("「", "」");
            jpDict.Add("『", "』");

            if (data != "")
            {
                if (separators.Any(s => data.Contains(s)))
                {
                    string separator = separators.Where(s => data.Contains(s)).ElementAt(0);

                    int index = data.Trim().IndexOf(separator);
                    song.Artist = data.Trim().Substring(0, index).Replace(new string[] { " x ", " ft. ", " vs ", " feat ", " feat. " }, " & ");
                    string[] keyWords = new string[] { "ft.", "feat.", "(ft.", "(feat" };

                    if (keyWords.Any(f => data.Contains(f)))
                    {
                        string ft = data.Split(keyWords, StringSplitOptions.RemoveEmptyEntries)[1].Replace(")", "").TrimStart().TrimEnd();
                        if (ft.Length > 0 && data.Trim().IndexOf(ft) > index)
                        {
                            song.Artist += " & " + ft;
                            song.Title = data.Split(keyWords, StringSplitOptions.RemoveEmptyEntries)[0].Trim().Substring(index + 3);
                        }
                        else
                        {
                            index = data.Trim().IndexOf(separator);
                            song.Title = data.Trim().Substring(index + 3);
                        }
                    }
                    else
                    {
                        index = data.Trim().IndexOf(separator);
                        song.Title = data.Trim().Substring(index + 3);
                    }
                }
                else if (jpDict.Keys.Any(s => data.Contains(s)))
                {
                    string match = jpDict.Keys.Where(s => data.Contains(s)).ElementAt(0);

                    int index = data.IndexOf(match);
                    int endOfTitle = data.IndexOf(jpDict[match]);
                    song.artist = data.Split(new string[] { match }, StringSplitOptions.None)[0].Trim();
                    song.title = data.Substring(index + 1, endOfTitle - index - 1).Trim();
                }

            }
            if (song.title.Contains(" - ")) song.title = song.title.Split(new string[] { " - " }, StringSplitOptions.None)[0];

            return song;
        }
    }

}
