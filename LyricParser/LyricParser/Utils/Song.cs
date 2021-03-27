using LyricParser.Common;
using System;
using System.Windows.Media.Imaging;

namespace LyricParser.Utils
{
    public struct Song
    {
        public string Title { get; set; }

        public string Title_EN { get; set; }

        public string Artist { get; set; }

        public Category Genre { get; set; }

        public BitmapSource Thumbnail { get; set; }

        public Song(string title, string title_en, string artist, Category genre, BitmapSource thumbnail)
        {
            Title = title;
            Title_EN = title_en;
            Artist = artist;
            Genre = genre;
            Thumbnail = thumbnail;
        }

        public Song(string info, string title_en, Category genre, BitmapSource thumbnail)
        {
            string[] artistTitle = info.Split('-');
            Artist = artistTitle[0].Trim();
            Title = artistTitle[1].Trim();
            Title_EN = title_en;
            Genre = genre;
            Thumbnail = thumbnail;
        }

        public static Song Empty()
        {
            BitmapImage bitmap = new BitmapImage(new Uri("pack://application:,,,/LyricParser;component/Resources/icon.png"));
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.Freeze();

            Song dummy = new Song
            {
                Genre = Category.None,
                Artist = "Null",
                Title = "Null",
                Thumbnail = bitmap
            };
            return dummy;
        }

        public static string[] FromString(string data)
        {
            return data.Split('-');
        }
    }
}
