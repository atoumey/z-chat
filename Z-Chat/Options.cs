using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.ComponentModel;

namespace ZChat
{
    public class ChatOptions : INotifyPropertyChanged
    {
        public string FirstChannel;
        public string FirstChannelKey;
        public string InitialNickname;
        public string Server;
        public int ServerPort;
        public bool SaveConnectionInfo = true;
        public SolidColorBrush EntryBack { get { return _entryBack; } set { _entryBack = value; FirePropertyChanged("EntryBack"); } }
        private SolidColorBrush _entryBack = Brushes.White;
        public SolidColorBrush EntryFore { get { return _entryFore; } set { _entryFore = value; FirePropertyChanged("EntryFore"); } }
        private SolidColorBrush _entryFore = Brushes.Black;
        public SolidColorBrush ChatBack { get { return _chatBack; } set { _chatBack = value; FirePropertyChanged("ChatBack"); } }
        private SolidColorBrush _chatBack = Brushes.White;
        public SolidColorBrush TimeFore = Brushes.Black;
        public SolidColorBrush NickFore = Brushes.Black;
        public SolidColorBrush BracketFore = Brushes.Black;
        public SolidColorBrush TextFore { get { return _textFore; } set { _textFore = value; FirePropertyChanged("TextFore"); } }
        public SolidColorBrush _textFore = Brushes.Black;
        public SolidColorBrush QueryTextFore = Brushes.Maroon;
        public SolidColorBrush OwnNickFore = Brushes.Green;
        public SolidColorBrush LinkFore { get { return _linkFore; } set { _linkFore = value; FirePropertyChanged("LinkFore"); } }
        public SolidColorBrush _linkFore = Brushes.Black;
        public SolidColorBrush UsersBack { get { return _usersBack; } set { _usersBack = value; FirePropertyChanged("UsersBack"); } }
        private SolidColorBrush _usersBack = Brushes.White;
        public SolidColorBrush UsersFore { get { return _usersFore; } set { _usersFore = value; FirePropertyChanged("UsersFore"); } }
        private SolidColorBrush _usersFore = Brushes.Black;
        public string TimeStampFormat = "HH:mm:ss ";
        public ClickRestoreType RestoreType { get { return _restoreType; } set { if (_restoreType != value) { _restoreType = value; FirePropertyChanged("RestoreType"); } } }
        private ClickRestoreType _restoreType = ClickRestoreType.SingleClick;
        public bool HighlightTrayIconForJoinsAndQuits = true;
        public FontFamily Font { get { return _font; } set { _font = value; FirePropertyChanged("Font"); } }
        private FontFamily _font = new FontFamily("Courier New");
        public bool WindowsForPrivMsgs = false;
        public string HyperlinkPattern { get { return _hyperlinkPattern; } set { _hyperlinkPattern = value; FirePropertyChanged("HyperlinkPattern"); } }
        protected string _hyperlinkPattern = "(^|[ ]|((https?|ftp):\\/\\/))(([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+)|localhost|([a-zA-Z0-9\\-]+\\.)*[a-zA-Z0-9\\-]+\\.(com|net|org|info|biz|gov|name|edu|[a-zA-Z][a-zA-Z]))(:[0-9]+)?((\\/|\\?)[^ \"]*[^ ,;\\.:\">)])?\\)?";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
