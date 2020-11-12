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

        const string lpClassName = "Winamp v1.x";
        const string strTtlEnd = " - Winamp";
        const Category DEBUG_MODE = Category.None;

        public string Title { get; set; }

        public string Title_EN { get; set; }

        public string Artist { get; set; }

        public Category Genre { get; set; }

        public Song(string title, string title_en, string artist, Category genre)
        {
            Title = title;
            Title_EN = title_en;
            Artist = artist;
            Genre = genre;
        }

        public static Song Empty()
        {
            Song dummy = new Song
            {
                Genre = Category.None,
                Artist = "Null",
                Title = "Null"
            };
            return dummy;
        }

        public static Song GetSongInfo()
        {
            if (DEBUG_MODE == Category.Touhou)
            {
                Song dummy = new Song
                {
                    Genre = Category.Touhou,
                    Artist = "柿チョコ",
                    Title = "CONFINED INNOCENT"
                };
                return dummy;
            }
            else if (DEBUG_MODE == Category.Anime)
            {
                Song dummy = new Song
                {
                    Genre = Category.Anime,
                    Artist = "Supercell",
                    Title = "Kimi no Shiranai Monogatari"
                };
                return dummy;
            }
            else if(DEBUG_MODE == Category.Western)
            {
                Song dummy = new Song
                {
                    Genre = Category.Western,
                    Artist = "Paramore",
                    Title = "Decode"
                };
                return dummy;
            }
            else if (DEBUG_MODE == Category.JP)
            {
                Song dummy = new Song
                {
                    Genre = Category.JP,
                    Artist = "高橋優",
                    Title = "福笑い"
                };
                return dummy;
            }

            switch (Player.Winamp)
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

            Song song = new Song
            {
                Genre = Category.Western
            };
            return song;
        }

        public override string ToString()
        {
            return String.Format("Artist: {0}" + " Title: {1}", Artist, Title);
        }

        // Get winamp info from the window handle using P/Invoke
        public static Song GetWinampInfo()
        {
            Song song = new Song
            {
                Genre = Category.Western
            };

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

        // Get spotify info from the main window corresponding to the spotify process
        public static Song GetSpotifyInfo()
        {
            Song song = new Song
            {
                Genre = Category.Western
            };

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

        // Get youtube info from the main window corresponding to the chrome process (Youtube must be the current tab)
        public static Song GetYoutubeInfo()
        {
            Song song = new Song
            {
                Genre = Category.Western
            };

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

        // Get Google Play Music info from the main window corresponding to the chrome process (Google Play Music must be the current tab)
        public static Song GetGooglePlayMusicInfo()
        {
            Song song = new Song
            {
                Genre = Category.Western
            };

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

        // Cleanup song info by removing unnecessary information
        private static Song CleanUpInfo(string data)
        {
            Song song = new Song
            {
                Genre = Category.Western
            };

            data = data.Split(new string[] { " / " }, StringSplitOptions.None)[0];
            data = data.RemoveBracket('[');
            data = data.RemoveBracket('(', new string[] { "(ft.", "(feat" });
            data = data.Replace(new string[] { "(", ")" }, "");
            data = data.Replace("*", "");
            data = data.Replace(": ", " : ");
            data = data.Replace("\"", "");
            data = data.Replace("Official Music Video", "");
            data = data.Replace("Music Video", "");
            data = data.Replace("Official Video", "");
            data = data.Replace("Dance Video", "");

            string[] separators = new string[] { " - ", " – ", " : " };
            string[] jpSeparators = new string[] { "「", "『" };
            Dictionary<string, string> jpDict = new Dictionary<string, string>
            {
                { "「", "」" },
                { "『", "』" }
            };

            if (data != "")
            {
                try
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

                        if (match.Any(m => data.Contains(m)))
                            song.Artist = data.Split(new string[] { match }, StringSplitOptions.None)[0].Trim();
                        song.Title = data.Substring(index + 1, endOfTitle - index - 1).Trim();
                    }
                }
                catch(Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }
            if (song.Title != null && song.Title.Contains(" - ")) song.Title = song.Title.Split(new string[] { " - " }, StringSplitOptions.None)[0];

            return song;
        }
    }

}
