using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;

namespace LyricParser
{
    public class ViewModel : INotifyPropertyChanged
    {
        private string dbFile = @"data.db";
        private ObservableCollection<HistoryEntry> searchHistory = new ObservableCollection<HistoryEntry>();
        public ICollectionView SearchHistoryView { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public ViewModel()
        {

            DatabaseHandler.database = string.Format(@"Data Source={0}; Pooling=false; FailIfMissing=false;", dbFile);

            if (!File.Exists(dbFile))
            {
                DatabaseHandler.CreateDB(dbFile);
            }

            SearchHistory = DatabaseHandler.GetSearchHistory();
        }


        public void AddHistoryEntry(HistoryEntry data)
        {
            SearchHistory.Insert(0, data);
            UpdatePosition();

            DatabaseHandler.UpdateSearchHistory(SearchHistory);
            NotifyPropertyChanged("SearchHistoryView");
        }

        public void UpdatePosition()
        {
            foreach(HistoryEntry data in SearchHistory)
            {
                data.Position = SearchHistory.IndexOf(data);
            }
        }
        public ObservableCollection<HistoryEntry> SearchHistory
        {
            get
            {
                return searchHistory;
            }
            private set
            {
                searchHistory = value;
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class HistoryEntry : IComparable<HistoryEntry>
    {
        public int Id { get; set; }
        public string Artist { get; private set; }
        public string Title { get; private set; }
        public int Position { get; set; }

        public string Data
        {
            get
            {
                return Artist + " - " + Title;
            }
            set
            {
                string[] data = value.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                Artist = data[0];
                Title = data[1];
            }
        }

        public void SetData(string artist, string title)
        {
            Artist = artist;
            Title = title;
        }

        public int CompareTo(HistoryEntry comparePart)
        {
            if (comparePart == null)
                return 1;

            else
                return this.Position.CompareTo(comparePart.Position);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is HistoryEntry))
                return false;
            return ((HistoryEntry)obj).Data == Data;
        }

        public override int GetHashCode()
        {
            return Id ^ 7;
        }
    }
}
