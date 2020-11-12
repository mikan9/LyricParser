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

        // Create a new database and fill it with tables
        public static void CreateDB()
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
                            Trace.WriteLine(e.ToString());
                        }
                    }

                }
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                }
            }
        }

        // Create and return a SQLite connection
        public static SQLiteConnection CreateConnection(string db)
        {
            return new SQLiteConnection(db);
        }

        // Update table containing last song (Temporary solution)
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
                        Trace.WriteLine(e.ToString());
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

        // Get last song from the corresponding table
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

        // Update search history by inserting a new record if possible else update corresponding record (Temporary "upsert"-like solution)
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
                            Trace.WriteLine(e.ToString());
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
                            Trace.WriteLine(e.ToString());
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

        // Get search history from corresponding table
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

        // Insert lyrics and song info if not exists else update
        public static bool AddSong(string artist, string title, string title_en, Category genre, List<string> lyrics, bool exist)
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
                            "VALUES ('" + artist.Replace("'", "''") + "', '" + title.Replace("'", "''") + "', '" + title_en.Replace("'", "''") + "', " + (int)genre + ", '" + lyrics[0].Replace("'", "''") + "', '" + lyrics[1].Replace("'", "''") + "', '" + lyrics[2].Replace("'", "''") + "');";
                    }
                    else
                    {
                        cmd.CommandText =
                            "UPDATE Song SET Lyrics_Kanji = '" + lyrics[0].Replace("'", "''") + "', Lyrics_Romaji = '" + lyrics[1].Replace("'", "''") + "', Lyrics_English = '" + lyrics[2].Replace("'", "''") + "' WHERE Artist = '" + artist + "' AND Title = '" + title + "';";
                    }
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch(SQLiteException e)
                    {
                        Trace.WriteLine(e.ToString());
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

        // Get english title of song if exists (Used for Japanese songs)
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

        // Load stored lyrics and song info
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

        // Check table if lyrics already exists
        public static bool LyricsExist(Song song, bool showLyrics, MainWindowView main)
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
                                //main.SetLyrics(dr["Lyrics_Kanji"].ToString(), dr["Lyrics_Romaji"].ToString(), dr["Lyrics_English"].ToString());
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
