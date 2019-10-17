using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricParser
{
    static class DatabaseHandler
    {
        public static string database = "";

        public static void CreateDB()
        {
            using (var dbConn = createConnection(database))
            {
                dbConn.Open();
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Songs (ID integer primary key, Artist text, Title text, Title_EN text, Genre text, Lyrics_Kanji text, Lyrics_Romaji text, Lyrics_English text);";
                    cmd.ExecuteNonQuery();

                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }
        }
        public static SQLiteConnection createConnection(string db)
        {
            return new SQLiteConnection(db);
        }
        public static bool AddSong(string artist, string title, string title_en, Category genre, List<string> lyrics, bool exist, string id)
        {
            bool success = true;

            using (var dbConn = createConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    if (!exist)
                    {
                        cmd.CommandText =
                            "INSERT INTO Songs ([Artist], [Title], [Title_EN], [Genre], [Lyrics_Kanji], [Lyrics_Romaji], [Lyrics_English]) VALUES (@Artist, @Title, @Title_EN, @Genre, @LyricsKanji, @LyricsRomaji, @LyricsEnglish);";

                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@Artist",
                            Value = artist
                        });

                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@Title",
                            Value = title
                        });

                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@Title_EN",
                            Value = title_en
                        });

                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@Genre",
                            Value = (int)genre
                        });

                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@LyricsKanji",
                            Value = lyrics[0]
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@LyricsRomaji",
                            Value = lyrics[1]
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = "@LyricsEnglish",
                            Value = lyrics[2]
                        });
                    }
                    else
                    {
                        cmd.CommandText =
                            "UPDATE Songs SET Lyrics_Kanji = :LyricsKanji, Lyrics_Romaji = :LyricsRomaji, Lyrics_English = :LyricsEnglish WHERE Artist = :artist AND Title = :title";

                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = ":LyricsKanji",
                            Value = lyrics[0]
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = ":LyricsRomaji",
                            Value = lyrics[1]
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = ":LyricsEnglish",
                            Value = lyrics[2]
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = ":artist",
                            Value = artist
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = ":title",
                            Value = title
                        });
                        cmd.Parameters.Add(new System.Data.SQLite.SQLiteParameter
                        {
                            ParameterName = ":id",
                            Value = id
                        });
                    }
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch(SQLiteException e)
                    {
                        Trace.WriteLine(e.Message);
                        success = false;
                    }
                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                    
                }
            }
            return success;
        }

        public static void GetEnglishTitle(Song song)
        {
            if (!Properties.Settings.Default.UseDatabase) return;
            song.Title_EN = "";

            using (var dbConn = createConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Songs";
                    SQLiteDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        if (dr["Artist"].ToString() == song.Artist && dr["Title"].ToString() == song.Title)
                        {
                            song.Title_EN = dr["Title_EN"].ToString();
                        }
                    }
                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }
        }

        public static void LoadLyrics()
        {
            using (var dbConn = createConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Songs";
                    SQLiteDataReader dr = cmd.ExecuteReader();
                    int i = 0;

                    List<Lyric> lyrics = new List<Lyric>();
                    while (dr.Read())
                    {
                        lyrics.Add(new Lyric()
                        {
                            artist = dr["Artist"].ToString(),
                            title = dr["Title"].ToString(),
                            genre = (Category)int.Parse(dr["Genre"].ToString()),
                            original = dr["Lyrics_Kanji"].ToString(),
                            romaji = dr["Lyrics_Romaji"].ToString(),
                            translation = dr["Lyrics_English"].ToString()
                        });
                        i++;
                    }
                    LyricHandler.SaveLyrics(lyrics);
                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }
        }

        public static bool lyricsExist(Song song, bool showLyrics, MainWindow main)
        {
            if (!Properties.Settings.Default.UseDatabase) return false;
            bool foundLyrics = false;

            using (var dbConn = createConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Songs";

                    SQLiteDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        if (dr["Artist"].ToString() == song.Artist && dr["Title"].ToString() == song.Title)
                        {
                            if (showLyrics)
                            {
                                main.SetLyrics(dr["Lyrics_Kanji"].ToString(), dr["Lyrics_Romaji"].ToString(), dr["Lyrics_English"].ToString(), (Category)int.Parse(dr["Genre"].ToString()));
                            }
                            //currentSongID = dr["ID"].ToString();
                            foundLyrics = true;

                            Trace.WriteLine(foundLyrics);
                        }
                    }

                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }

            return foundLyrics;
        }
    }
}
