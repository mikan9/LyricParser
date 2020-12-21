using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LyricParser.Common;
using LyricParser.Extensions;
using LyricParser.Models;
using SQLite;

namespace LyricParser.Repository
{
    public class LyricsDatabase
    {
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized = false;

        public LyricsDatabase()
        {
            InitializeAsync().SafeFireAndForget(false);
        }

        async Task InitializeAsync()
        {
            if (!initialized)
            {
                if(!Database.TableMappings.Any(m => m.MappedType.Name == typeof(Lyrics).Name))
                {
                    await Database.CreateTablesAsync(CreateFlags.None, typeof(Lyrics)).ConfigureAwait(false);
                }
                initialized = true;
            }
        }

        public async Task DropTable()
        {
            if(Database.TableMappings.Any(m => m.MappedType.Name == typeof(Lyrics).Name))
            {
                await Database.DropTableAsync<Lyrics>();
            }
        }

        public Task<List<Lyrics>> GetAllLyricsAsync()
        {
            return Database.Table<Lyrics>().ToListAsync();
        }

        public Task<Lyrics> GetLyricsAsync(string artist, string title)
        {
            return Database.Table<Lyrics>().Where(i => i.Artist == artist && i.Title == title).FirstOrDefaultAsync();
        }

        public async Task SaveLyricsAsync(Lyrics entry)
        {
            var lyrics = await GetLyricsAsync(entry.Artist, entry.Title);

            if (lyrics == null)
                await Database.InsertAsync(entry);
            else 
                await Database.UpdateAsync(new Lyrics()
                {
                    ID = lyrics.ID,
                    Artist = lyrics.Artist,
                    Title = lyrics.Title,
                    Content = entry.Content
                });
        }

        public Task<int> DeleteLyricsAsync(Lyrics entry)
        {
            return Database.DeleteAsync(entry);
        }
    }
}
