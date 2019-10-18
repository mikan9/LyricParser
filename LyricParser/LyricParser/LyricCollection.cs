using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricParser
{
    [Serializable]
    public class LyricCollection
    {
        private ArrayList _lyrics = null;

        public LyricCollection()
        {
            _lyrics = new ArrayList();
        }

        public IEnumerator Lyrics
        {
            get
            {
                return _lyrics.GetEnumerator();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _lyrics.GetEnumerator();
        }

        public void AddLyric(int id, Lyric _lyric)
        {
            _lyrics.Add(new DictionaryEntry(id, _lyric));
        }

        public void RemoveLyric(int index)
        {
            _lyrics.RemoveAt(index);
        }

        public void ClearLyrics()
        {
            _lyrics.Clear();
        }

        public int LyricsCount
        {
            get
            {
                return _lyrics.Count;
            }
        }
    }

}
