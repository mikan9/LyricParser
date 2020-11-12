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
        public static readonly RoutedUICommand GetCurrentSong = new RoutedUICommand(LocaleResources.GetCurrentSong, "GetCurrentSong", typeof(MainWindowView));
        public static readonly RoutedUICommand SearchInBrowser = new RoutedUICommand(LocaleResources.SearchInBrowser, "SearchInBrowser", typeof(MainWindowView));
        public static readonly RoutedUICommand ClearSearchHistory = new RoutedUICommand(LocaleResources.ClearSearchHistory, "ClearSearchHistory", typeof(MainWindowView));
    }
}
