using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricParser
{
    static class DatabaseHandler
    {
        public static string database = "";

        public static void CreateDB(string path)
        {
            using (var dbConn = CreateConnection(database))
            {
                dbConn.Open();
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    using (SQLiteTransaction transaction = dbConn.BeginTransaction())
                    {
                        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Song (ID integer primary key, Artist text, Title text, Title_EN text, Genre text, Lyrics_Kanji text, Lyrics_Romaji text, Lyrics_English text);
                                        CREATE TABLE IF NOT EXISTS SearchHistory (ID integer primary key, Artist text, Title text, Position integer, UNIQUE(Artist, Title));
                                        CREATE TABLE IF NOT EXISTS LastSong (ID integer primary key, Artist text, Title text);
                                        INSERT INTO LastSong (ID, Artist, Title) VALUES (1, 'null', 'null');";
                        try
                        {
                            cmd.ExecuteNonQuery();
                            transaction.Commit();
                        }
                        catch(SQLiteException e)
                        {
                            transaction.Rollback();
                        }
                    }

                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }
        }
        public static SQLiteConnection CreateConnection(string db)
        {
            return new SQLiteConnection(db);
        }
        public static bool UpdateLastSong(string artist, string title)
        {
            bool success = true;
            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText =
                             "UPDATE LastSong SET Artist = '" + artist + "', Title = '" + title + "' WHERE ID = 1";
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SQLiteException e)
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
        public static HistoryEntry GetLastSong()
        {
            HistoryEntry lastSong = new HistoryEntry();
            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM LastSong WHERE ID LIKE 1";
                    SQLiteDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        lastSong.SetData(dr["Artist"].ToString(), dr["Title"].ToString());
                    }
                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }

            return lastSong;

        }
        public static bool UpdateSearchHistory(ObservableCollection<HistoryEntry> searchHistory)
        {
            bool success = true;
            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    using (SQLiteTransaction transaction = dbConn.BeginTransaction())
                    {
                        for(int i = 0; i < searchHistory.Count; ++i) 
                        {
                            HistoryEntry historyEntry = searchHistory.ElementAt(i);
                            cmd.CommandText =
                                "INSERT OR IGNORE INTO SearchHistory (Artist, Title, Position) " +
                                "VALUES ('" + historyEntry.Artist + "', '" + historyEntry.Title + "', " + historyEntry.Position + ");";

                            cmd.ExecuteNonQuery();
                        }

                        try
                        {
                            transaction.Commit();
                        }
                        catch (SQLiteException e)
                        {
                            Trace.WriteLine(e.Message);
                            success = false;
                        }
                    }
                    using (SQLiteTransaction transaction = dbConn.BeginTransaction())
                    {
                        for (int i = 0; i < searchHistory.Count; ++i)
                        {
                            HistoryEntry historyEntry = searchHistory.ElementAt(i);
                            cmd.CommandText =
                                "UPDATE SearchHistory SET Position = " + historyEntry.Position +
                                " WHERE Artist = '" + historyEntry.Artist + "' AND Title = '" + historyEntry.Title + "';";

                            cmd.ExecuteNonQuery();
                        }

                        try
                        {
                            transaction.Commit();
                        }
                        catch (SQLiteException e)
                        {
                            Trace.WriteLine(e.Message);
                            success = false;
                        }
                    }
                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }
            return success;
        }
        public static ObservableCollection<HistoryEntry> GetSearchHistory()
        {
            ObservableCollection<HistoryEntry> searchHistory = new ObservableCollection<HistoryEntry>();
            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM SearchHistory ORDER BY Position";
                    SQLiteDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        searchHistory.Add(new HistoryEntry() { Data = dr["Artist"].ToString() + " - " + dr["Title"].ToString(), Position = int.Parse(dr["Position"].ToString()) });
                    }
                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }

            return searchHistory;
        }
        public static bool AddSong(string artist, string title, string title_en, Category genre, List<string> lyrics, bool exist, string id)
        {
            bool success = true;

            using (var dbConn = CreateConnection(database))
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
                            "INSERT INTO Song (Artist, Title, Title_EN, Genre, Lyrics_Kanji, Lyrics_Romaji, Lyrics_English) " +
                            "VALUES ('" + artist + "', '" + title + "', '" + title_en + "', " + genre + ", '" + lyrics[0] + "', '" + lyrics[1] + "', '" + lyrics[2] + "');";
                    }
                    else
                    {
                        cmd.CommandText =
                            "UPDATE Song SET Lyrics_Kanji = '" + lyrics[0] + "', Lyrics_Romaji = '" + lyrics[1] + "', Lyrics_English = '" + lyrics[2] + "' WHERE Artist = '" + artist + "' AND Title = '" + title + "';";
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

            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Song";
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
            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Song";
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

        public static bool LyricsExist(Song song, bool showLyrics, MainWindow main)
        {
            if (!Properties.Settings.Default.UseDatabase) return false;
            bool foundLyrics = false;

            using (var dbConn = CreateConnection(database))
            {
                if (dbConn.State != System.Data.ConnectionState.Open)
                {
                    dbConn.Open();
                }
                using (SQLiteCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Song";

                    SQLiteDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        if (dr["Artist"].ToString() == song.Artist && dr["Title"].ToString() == song.Title)
                        {
                            if (showLyrics)
                            {
                                main.SetLyrics(dr["Lyrics_Kanji"].ToString(), dr["Lyrics_Romaji"].ToString(), dr["Lyrics_English"].ToString(), (Category)int.Parse(dr["Genre"].ToString()));
                            }
                            foundLyrics = true;
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
