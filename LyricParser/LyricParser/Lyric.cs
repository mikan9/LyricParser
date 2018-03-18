using System;
using System.Collections.Generic;
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
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, lyrics);
                ms.Position = 0;
                byte[] buffer = new byte[(int)ms.Length];
                ms.Read(buffer, 0, buffer.Length);
                Properties.Settings.Default.Lyrics = Convert.ToBase64String(buffer);
                Properties.Settings.Default.Save();
            }
        }

        public static List<Lyric> LoadLyrics()
        {
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(Properties.Settings.Default.Lyrics)))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return (List<Lyric>)bf.Deserialize(ms);
            }
        }
    }
}
