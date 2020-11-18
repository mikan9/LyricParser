using SQLite;

namespace LyricParser.Models
{
    public class Lyrics
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        [Indexed(Name = "LyricsID", Order = 1, Unique = true)]
        public string Artist { get; set; }
        [Indexed(Name = "LyricsID", Order = 2, Unique = true)]
        public string Title { get; set; }
        public string Content { get; set; }

        public static Lyrics Empty()
        {
            return new Lyrics()
            {
                ID = -1,
                Artist = "",
                Title = "",
                Content = ""
            }; 
        }
    }
}
