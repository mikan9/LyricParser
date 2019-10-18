using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LyricParser
{
    [Serializable]
    public struct Lyric
    {
        public string artist;
        public string title;
        public Category genre;
        public string original;
        public string romaji;
        public string translation;
    }

    public static class LyricHandler
    {
        private static List<Lyric> SavedLyrics = new List<Lyric>();
        public static Lyric CreateLyric(string artist, string title, Category genre, string original, string romaji, string translation)
        {
            return new Lyric()
            {
                artist = artist,
                title = title,
                genre = genre,
                original = original,
                romaji = romaji,
                translation = translation
            };
        }

        public static void SaveLyrics(List<Lyric> lyrics)
        {
            SavedLyrics = lyrics;
        }

        public static List<Lyric> GetLyrics()
        {
            return SavedLyrics;
        }
    }
}
