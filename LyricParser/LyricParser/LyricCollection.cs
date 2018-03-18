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

        public System.Collections.IEnumerator Lyrics
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
            //int id = 0;
            //if (LyricsCount > 0) id = (int)((DictionaryEntry)_lyrics[LyricsCount - 1]).Key + 1;
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
