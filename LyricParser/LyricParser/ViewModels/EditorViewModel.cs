using LyricParser.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace LyricParser.ViewModels
{
    public class EditorViewModel : DialogBase
    {
        private List<Key> keysDown = new List<Key>();
        private readonly double defFontSize = 16.0;
        private readonly double zoomStep = 25.0;
        private readonly int zoomLevels = 8;

        private Lyrics lyrics = Lyrics.Empty();

        private string _songArtist = "";
        private string _songTitle = "";
        private string _content = "";

        private int _zoomSelectionIndex = 3;

        private double _lyricsFontSize = 14;


        #region Properties
        // String properties
        public string SongArtist
        {
            get => _songArtist;
            set => SetProperty(ref _songArtist, value);
        }
        public string SongTitle
        {
            get => _songTitle;
            set => SetProperty(ref _songTitle, value);
        }
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        // Int properties
        public int ZoomSelectionIndex
        {
            get => _zoomSelectionIndex;
            set => SetProperty(ref _zoomSelectionIndex, value);
        }

        // Double properties
        public double LyricsFontSize
        {
            get => _lyricsFontSize;
            set => SetProperty(ref _lyricsFontSize, value);
        }
        #endregion

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ViewLoadedCommand { get; }
        public DelegateCommand ViewClosingCommand { get; }
        public DelegateCommand<MouseWheelEventArgs> ViewMouseWheelCommand { get; }
        public DelegateCommand<KeyEventArgs> ViewKeyDownCommand { get; }
        public DelegateCommand<KeyEventArgs> ViewKeyUpCommand { get; }

        public DelegateCommand<SelectionChangedEventArgs> ZoomSelectionChangedCommand { get; }

        #endregion

        IEventAggregator _eventAggregator { get; }

        public EditorViewModel(IEventAggregator ea)
        {
            _eventAggregator = ea;
            _title = "Lyrics Editor";

            SaveCommand = new DelegateCommand(Save);

            ViewLoadedCommand = new DelegateCommand(OnViewLoaded);
            ViewClosingCommand = new DelegateCommand(OnViewClosing);
            ViewMouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(OnViewMouseWheel);
            ViewKeyDownCommand = new DelegateCommand<KeyEventArgs>(OnViewKeyDown);
            ViewKeyUpCommand = new DelegateCommand<KeyEventArgs>(OnViewKeyUp);

            ZoomSelectionChangedCommand = new DelegateCommand<SelectionChangedEventArgs>(ZoomSelectionChanged);

            ZoomSelectionIndex = Properties.Settings.Default.ZoomIndex;
            SetFontSize();
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            lyrics = parameters.GetValue<Lyrics>("data");
            SongArtist = lyrics.Artist;
            SongTitle = lyrics.Title;
            Content = lyrics.Content;
        }

        private void Save()
        {
            lyrics.Content = Content;
            RaiseRequestClose(
                new DialogResult(ButtonResult.OK, 
                new DialogParameters() { { "data", lyrics } }
                ));
        }

        private void OnViewMouseWheel(MouseWheelEventArgs e)
        {
            if (keysDown.Contains(Key.LeftCtrl))
            {
                e.Handled = true;
                int d = e.Delta > 0 ? 1 : -1;
                ZoomSelectionIndex += ZoomSelectionIndex + d > zoomLevels || ZoomSelectionIndex + d < 0 ? 0 : d;
            }
        }

        private void ZoomSelectionChanged(SelectionChangedEventArgs e)
        {
            SetFontSize();
        }

        private void SetFontSize()
        {
            LyricsFontSize = defFontSize / 100 * (ZoomSelectionIndex * zoomStep + zoomStep);
        }

        private void OnViewLoaded()
        {
        }

        private void OnViewKeyDown(KeyEventArgs e)
        {
            if (!keysDown.Contains(e.Key))
            {
                keysDown.Add(e.Key);
            }
        }

        private void OnViewKeyUp(KeyEventArgs e)
        {
            keysDown.Remove(e.Key);
        }

        private void OnViewClosing()
        {
        }
    }
}
