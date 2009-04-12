using System;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.IO;
using Meebey.SmartIrc4net;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        private string FirstChannel;
        private string Nickname;
        private string Server;

        ChannelWindow FirstWindow;
        public IrcClient IRC = new IrcClient();

        private Dictionary<string, PrivMsg> queryWindows = new Dictionary<string, PrivMsg>();

        public SolidColorBrush EntryBack { get { return _entryBack; } set { _entryBack = value; FirePropertyChanged("EntryBack"); } }
        private SolidColorBrush _entryBack = Brushes.White;
        public SolidColorBrush EntryFore { get { return _entryFore; } set { _entryFore = value; FirePropertyChanged("EntryFore"); } }
        private SolidColorBrush _entryFore = Brushes.Black;
        public SolidColorBrush ChatBack { get { return _chatBack; } set { _chatBack = value; FirePropertyChanged("ChatBack"); } }
        private SolidColorBrush _chatBack = Brushes.White;
        public SolidColorBrush TimeFore = Brushes.Black;
        public SolidColorBrush NickFore = Brushes.Black;
        public SolidColorBrush BracketFore = Brushes.Black;
        public SolidColorBrush TextFore = Brushes.Black;
        public SolidColorBrush QueryTextFore = Brushes.Maroon;
        public SolidColorBrush OwnNickFore = Brushes.Green;
        public SolidColorBrush LinkFore = Brushes.Black;
        public SolidColorBrush UsersBack { get { return _usersBack; } set { _usersBack = value; FirePropertyChanged("UsersBack"); } }
        private SolidColorBrush _usersBack = Brushes.White;
        public SolidColorBrush UsersFore { get { return _usersFore; } set { _usersFore = value; FirePropertyChanged("UsersFore"); } }
        private SolidColorBrush _usersFore = Brushes.Black;
        public string TimeStampFormat = "HH:mm:ss ";
        public ClickRestoreType RestoreType { get { return _restoreType; } set { _restoreType = value; FirePropertyChanged("RestoreType"); } }
        private ClickRestoreType _restoreType = ClickRestoreType.SingleClick;
        public bool HighlightTrayIconForJoinsAndQuits = true;
        public new FontFamily Font { get { return _font; } set { _font = value; FirePropertyChanged("Font"); } }
        private FontFamily _font = new FontFamily("Courier New");
        public bool WindowsForPrivMsgs = false;
        public string LastFMUserName = "";

        public App()
        {
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(UnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);

            IRC.ActiveChannelSyncing = true;
            IRC.AutoNickHandling = true;
            IRC.SupportNonRfc = true;
            IRC.Encoding = System.Text.Encoding.UTF8;
            IRC.OnConnected += new EventHandler(IRC_OnConnected);
            IRC.OnQueryAction += new ActionEventHandler(IRC_OnQueryAction);
            IRC.OnQueryMessage += new IrcEventHandler(IRC_OnQueryMessage);
            IRC.OnQueryNotice += new IrcEventHandler(IRC_OnQueryNotice);
            IRC.OnNickChange += new NickChangeEventHandler(IRC_OnNickChange);

            FirstWindow = new ChannelWindow(this);
            FirstWindow.Closed += new EventHandler(Window_Closed);
            FirstWindow.Show();

            if (!File.Exists("config.txt"))
            {
                ShowConnectionWindow(FirstWindow);
            }
            else
            {
                ReadConfigFile();
            }

            FirstWindow.Channel = FirstChannel;
            
            if (Server != null)
                IRC.Connect(Server, 6667);
        }

        /// <summary>
        /// Catches the OnNickChange event so we can keep our list of query windows up-to-date.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IRC_OnNickChange(object sender, NickChangeEventArgs e)
        {
            List<string> keysToRemove = new List<string>();
            List<KeyValuePair<string, PrivMsg>> newPrivs = new List<KeyValuePair<string, PrivMsg>>();
            foreach (KeyValuePair<string, PrivMsg> pair in queryWindows)
                if (pair.Key == e.OldNickname)
                {
                    keysToRemove.Add(pair.Key);
                    newPrivs.Add(new KeyValuePair<string, PrivMsg>(e.NewNickname, pair.Value));
                }
            foreach (string s in keysToRemove)
                queryWindows.Remove(s);
            foreach (KeyValuePair<string, PrivMsg> pair in newPrivs)
                queryWindows.Add(pair.Key, pair.Value);
        }

        void IRC_OnQueryNotice(object sender, IrcEventArgs e)
        {
            DelegateIncomingQueryMessage(e.Data.Nick, e);
        }

        void IRC_OnQueryMessage(object sender, IrcEventArgs e)
        {
            DelegateIncomingQueryMessage(e.Data.Nick, e);
        }

        void IRC_OnQueryAction(object sender, ActionEventArgs e)
        {
            DelegateIncomingQueryMessage(e.Data.Nick, e);
        }

        private void DelegateIncomingQueryMessage(string nick, IrcEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data.Nick))
                FirstWindow.TakeIncomingQueryMessage(e);
            else if (!queryWindows.ContainsKey(nick))
            {
                if (WindowsForPrivMsgs)
                    Dispatcher.BeginInvoke(new VoidDelegate(delegate
                    {
                        PrivMsg priv = new PrivMsg(this, nick, e);
                        queryWindows.Add(e.Data.Nick, priv);
                        priv.Closed += new EventHandler(Query_Closed);
                        priv.Show();
                    }));
                else
                    FirstWindow.TakeIncomingQueryMessage(e);
            }
        }

        void Query_Closed(object sender, EventArgs e)
        {
            PrivMsg priv = sender as PrivMsg;
            if (priv != null) priv.Closed -= Query_Closed;

            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, PrivMsg> pair in queryWindows)
                if (pair.Value == sender)
                    keysToRemove.Add(pair.Key);
            foreach (string s in keysToRemove)
                queryWindows.Remove(s);

            Window_Closed(sender, e);
        }

        void IRC_OnConnected(object sender, EventArgs e)
        {
            IRC.Login(Nickname, "Real Name", 0, "username");
            new Thread(new ThreadStart(delegate { IRC.Listen(); })).Start();
        }

        void Window_Closed(object sender, EventArgs e)
        {
            if (this.Windows.Count == 0)
            {
                if (IRC.IsConnected)
                    IRC.Disconnect();

                Shutdown();
            }
        }

        private void ReadConfigFile()
        {
            throw new NotImplementedException();
        }

        private void ShowConnectionWindow(ChannelWindow FirstWindow)
        {
            ConnectionWindow connWin = new ConnectionWindow();
            connWin.Owner = FirstWindow;
            if (connWin.ShowDialog().Value)
            {
                FirstChannel = connWin.Channel;
                Nickname = connWin.Nickname;
                Server = connWin.Server;
            }
            else
            {
                Shutdown();
            }
        }

        void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
                if (ex is TargetInvocationException)
                    Error.ShowError(ex.InnerException);
                else
                    Error.ShowError(ex);
        }

        void UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is TargetInvocationException)
                Error.ShowError(e.Exception.InnerException);
            else
                Error.ShowError(e.Exception);
        }

        public static SolidColorBrush CreateBrushFromString(string colorString)
        {
            byte a = 255, r = 255, g = 255, b = 255;
            if (colorString.Length == 7)
            {
                a = 255;
                r = byte.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            }
            else if (colorString.Length == 9)
            {
                a = byte.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                r = byte.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(colorString.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ShowOptions()
        {
            Options optionsDialog = new Options(this);
            optionsDialog.ShowDialog();
        }

        public void SendQueryMessage(string nick, string message)
        {
            if (queryWindows.ContainsKey(nick))
                queryWindows[nick].TakeOutgoingMessage(message);
            else if (WindowsForPrivMsgs)
                Dispatcher.BeginInvoke(new VoidDelegate(delegate
                {
                    PrivMsg priv = new PrivMsg(this, nick, message);
                    queryWindows.Add(nick, priv);
                    priv.Closed += new EventHandler(Query_Closed);
                    priv.Show();
                }));
            else
                FirstWindow.TakeOutgoingQueryMessage(nick, message);
        }
    }

    delegate void VoidDelegate();
}
