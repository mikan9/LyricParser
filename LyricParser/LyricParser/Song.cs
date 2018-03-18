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
                    return Song.GetWinampInfo();
                case Player.Spotify:
                    return Song.GetSpotifyInfo();
                case Player.Youtube:
                    return Song.GetYoutubeInfo();
                case Player.GooglePlayMusic:
                    return Song.GetGooglePlayMusicInfo();
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

            if ((intLength <= 0) || (intLength > lpText.Length))
            {
                song.Title = "unknown";
                return song;
            }

            string strTitle = lpText.Substring(0, intLength);
            int intName = strTitle.IndexOf(strTtlEnd);
            int intLeft = strTitle.IndexOf("[");
            int intRight = strTitle.IndexOf("]");

            if ((intName >= 0) && (intLeft >= 0) && (intName < intLeft) && (intRight >= 0) && (intLeft + 1 < intRight))
            {
                //paused = true;
                song.Title = strTitle.Substring(intLeft + 1, intRight - intLeft - 1);
                return song;
            }
            else
            {
                //paused = false;
            }

            if ((strTitle.EndsWith(strTtlEnd)) && (strTitle.Length > strTtlEnd.Length))
            {
                strTitle = strTitle.Substring(0, strTitle.Length - strTtlEnd.Length);
            }

            int intDot = strTitle.IndexOf(".");
            if ((intDot > 0) && strTitle.Substring(0, intDot).IsNumeric())
            {
                strTitle = strTitle.Remove(0, intDot + 1);
            }

            int index = strTitle.Trim().IndexOf(" - ");
            if (index >= 0)
            {
                song.Artist = strTitle.Trim().Substring(0, index);
                song.Title = strTitle.Trim().Substring(index + 3);
                return song;
            }
            else
            {
                song.Title = strTitle.Trim();
                return song;
            }
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
                int index = spotifyName.Trim().IndexOf(" - ");
                song.Artist = spotifyName.Trim().Substring(0, index);
                song.Title = spotifyName.Trim().Substring(index + 3);
            }

            return song;
        }
        public static Song GetYoutubeInfo()
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

            if (tabName != "" && tabName.Contains(" - "))
            {
                int index = tabName.Trim().IndexOf(" - ");
                song.Artist = tabName.Trim().Substring(0, index);
                song.Title = tabName.Trim().Substring(index + 3);
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
                int index = tabName.Trim().IndexOf(" - ");
                song.Artist = tabName.Trim().Substring(0, index);
                song.Title = tabName.Trim().Substring(index + 3);
            }

            return song;
        }
    }

}
