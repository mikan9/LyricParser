using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LyricParser
{
    public static class Command
    {
        public static readonly RoutedUICommand GetCurrentSong = new RoutedUICommand("Get Current Song", "GetCurrentSong", typeof(MainWindow));
        public static readonly RoutedUICommand SearchInBrowser = new RoutedUICommand("Search In Browser", "SearchInBrowser", typeof(MainWindow));
    }
}
