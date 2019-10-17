using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LyricParser
{
    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            if (Properties.Settings.Default.SearchHistory == null)
                Properties.Settings.Default.SearchHistory = new ObservableCollection<HistoryEntry>();
            SearchHistory = Properties.Settings.Default.SearchHistory;
        }

        public ObservableCollection<HistoryEntry> SearchHistory
        {
            get
            {
                return Properties.Settings.Default.SearchHistory;
            }
            set
            {
                Properties.Settings.Default.SearchHistory = value;
                Properties.Settings.Default.Save();
                NotifyPropertyChanged("SearchHistory");
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [Serializable]
    public partial class HistoryEntry
    {
        public int Id { get; set; }
        public string Data { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is HistoryEntry))
                return false;
            return ((HistoryEntry)obj).Data == this.Data;
        }

        public override int GetHashCode()
        {
            return Id ^ 7;
        }
    }
}
