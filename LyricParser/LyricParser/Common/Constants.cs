using System;
using System.IO;

namespace LyricParser.Common
{
    public static class Constants
    {
        public const string DatabaseFilename = "LyricsCache.db3";

        public const string GENDOU_URL = "http://gendou.com/amusic/?filter=";
        public const string JLYRIC_URL = "http://search2.j-lyric.net/index.php?kt=";
        public const string TOUHOUWIKI_URL = "https://en.touhouwiki.net/wiki/Lyrics:_";
        public const string METROLYRICS_URL = "https://www.metrolyrics.com/printlyric/";
        public const string MUSIXMATCH_URL = "https://www.musixmatch.com/search/";
        public const string ATWIKI_URL = "https://www5.atwiki.jp/hmiku/?cmd=wikisearch&keyword=";

        public const SQLite.SQLiteOpenFlags Flags =
            SQLite.SQLiteOpenFlags.ReadWrite |
            SQLite.SQLiteOpenFlags.Create |
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LyricParser";
                return Path.Combine(basePath, DatabaseFilename);
            }
        }
    }
}
