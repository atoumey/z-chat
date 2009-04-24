using System;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.IO;
using Meebey.SmartIrc4net;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Text;
using System.Windows.Threading;
using System.Net;
using System.Xml.Linq;
using System.Linq;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        //If no config file is found (or the file does not contain connection info),
        // a connection dialog will be presented.
        //These will be the defaults for the connection dialog.
        public static string CONFIG_FILE_NAME = "zchat_config.txt";
        public static string FIRST_CHANNEL = "#test";
        public static string FIRST_CHANNEL_KEY;
        public static string INITIAL_NICKNAME = Environment.UserName;
        public static string SERVER_ADDRESS = "irc.mibbit.com";
        public static int SERVER_PORT = 6667;

        // The MainOutputWindow is where we output messages that are not specific to a particular
        // channel or query.
        ChannelWindow MainOutputWindow;
        public IrcClient IRC = new IrcClient();

        private Dictionary<string, PrivMsg> queryWindows = new Dictionary<string, PrivMsg>();
        private Dictionary<string, ChannelWindow> channelWindows = new Dictionary<string, ChannelWindow>();

        #region Options
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
        public FontFamily Font { get { return _font; } set { _font = value; FirePropertyChanged("Font"); } }
        private FontFamily _font = new FontFamily("Courier New");
        public bool WindowsForPrivMsgs = false;
        public string LastFMUserName = "";
        #endregion

        List<string> rawMessages = new List<string>();

        public App()
        {
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(UnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);

            MainOutputWindow = new ChannelWindow(this);
            MainOutputWindow.UserInput += ParseUserInput;
            MainOutputWindow.Closed += new EventHandler(channelWindow_Closed);
            MainOutputWindow.IsMainWindow = true;
            MainOutputWindow.Show();

            LoadConfigurationFile();

            bool proceed = true;
            if (FirstChannel == null || InitialNickname == null || Server == null)
            {
                if (FirstChannel == null) FirstChannel = FIRST_CHANNEL;
                if (FirstChannelKey == null) FirstChannelKey = FIRST_CHANNEL_KEY;
                if (InitialNickname == null) InitialNickname = INITIAL_NICKNAME;
                if (Server == null) Server = SERVER_ADDRESS;
                if (ServerPort == 0) ServerPort = SERVER_PORT;
                
                proceed = ShowConnectionWindow(MainOutputWindow);
            }

            if (proceed)
            {
                SaveConfigurationFile();

                IRC.ActiveChannelSyncing = true;
                IRC.AutoNickHandling = true;
                IRC.SupportNonRfc = true;
                IRC.Encoding = System.Text.Encoding.UTF8;
                IRC.OnConnected += new EventHandler(IRC_OnConnected);
                IRC.OnQueryAction += new ActionEventHandler(IRC_OnQueryAction);
                IRC.OnQueryMessage += new IrcEventHandler(IRC_OnQueryMessage);
                IRC.OnQueryNotice += new IrcEventHandler(IRC_OnQueryNotice);
                IRC.OnNickChange += new NickChangeEventHandler(IRC_OnNickChange);
                IRC.OnRawMessage += new IrcEventHandler(IRC_OnRawMessage);
                IRC.OnJoin += new JoinEventHandler(IRC_OnJoin);
                IRC.OnWriteLine += new WriteLineEventHandler(IRC_OnWriteLine);

                MainOutputWindow.Channel = FirstChannel;
                MainOutputWindow.ChannelKey = FirstChannelKey;

                channelWindows.Add(MainOutputWindow.Channel, MainOutputWindow);

                IRC.Connect(Server, ServerPort);
            }
        }

        void IRC_OnWriteLine(object sender, WriteLineEventArgs e)
        {
            rawMessages.Add(DateTime.Now.ToString("HH:mm:ss.ffff ") + e.Line);
        }

        void IRC_OnJoin(object sender, JoinEventArgs e)
        {
            // this is how we know the server has successfully joined us to a channel
            if (e.Who == IRC.Nickname && !channelWindows.ContainsKey(e.Channel))
            {
                Dispatcher.Invoke(new VoidDelegate(delegate
                {
                    ChannelWindow newWindow = new ChannelWindow(this);
                    newWindow.Channel = e.Channel;
                    newWindow.UserInput += ParseUserInput;

                    newWindow.Closed += channelWindow_Closed;
                    channelWindows.Add(e.Channel, newWindow);
                    newWindow.Show();
                }));
            }
        }

        void IRC_OnRawMessage(object sender, IrcEventArgs e)
        {
            rawMessages.Add(DateTime.Now.ToString("HH:mm:ss.ffff ") + e.Data.RawMessage);
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

        #region Private Queries
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
            if (string.IsNullOrEmpty(nick))
                MainOutputWindow.TakeIncomingQueryMessage(e);
            else if (!queryWindows.ContainsKey(nick))
            {
                if (WindowsForPrivMsgs)
                    Dispatcher.Invoke(new VoidDelegate(delegate { CreateNewPrivWindow(nick, e); }));
                else
                    MainOutputWindow.TakeIncomingQueryMessage(e);
            }
        }

        private void CreateNewPrivWindow(string nick, string message)
        {
            PrivMsg priv = new PrivMsg(this, nick, message);
            SetupNewPrivWindow(priv, nick);
        }

        private void CreateNewPrivWindow(string nick, IrcEventArgs e)
        {
            PrivMsg priv = new PrivMsg(this, nick, e);
            SetupNewPrivWindow(priv, nick);
        }

        private void SetupNewPrivWindow(PrivMsg priv, string nick)
        {
            priv.UserInput += ParseUserInput;
            queryWindows.Add(nick, priv);
            priv.Closed += new EventHandler(Query_Closed);
            priv.Show();
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

        public void SendQueryMessage(string nick, string message)
        {
            if (queryWindows.ContainsKey(nick))
                queryWindows[nick].TakeOutgoingMessage(message);
            else if (WindowsForPrivMsgs)
                Dispatcher.BeginInvoke(new VoidDelegate(delegate { CreateNewPrivWindow(nick, message); }));
            else
                MainOutputWindow.TakeOutgoingQueryMessage(nick, message);
        }
        #endregion

        void IRC_OnConnected(object sender, EventArgs e)
        {
            IRC.Login(InitialNickname, "Real Name", 0, "username");
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

        // Used for storing colors to the conig file
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

        #region Config File Load/Save
        private void LoadConfigurationFile()
        {
            try
            {
                if (File.Exists(CONFIG_FILE_NAME))
                {
                    string[] configContents = File.ReadAllLines(CONFIG_FILE_NAME);

                    Dictionary<string, string> options = new Dictionary<string, string>();
                    foreach (string line in configContents)
                    {
                        int colonPlace = line.IndexOf(':');
                        string optionName = line.Substring(0, colonPlace);
                        options.Add(optionName, line.Substring(colonPlace + 1, line.Length - colonPlace - 1));
                    }

                    if (options.ContainsKey("FirstChannel"))
                        FirstChannel = options["FirstChannel"];

                    if (options.ContainsKey("Nickname"))
                        InitialNickname = options["Nickname"];
                    if (options.ContainsKey("Server"))
                        Server = options["Server"];
                    if (options.ContainsKey("ServerPort"))
                    {
                        try { ServerPort = int.Parse(options["ServerPort"]); }
                        catch { ServerPort = 6667; }
                    }
                    if (options.ContainsKey("FirstChannelKey"))
                        FirstChannelKey = options["FirstChannelKey"];

                    if (options.ContainsKey("SaveConnectionInfo"))
                    {
                        if (options["SaveConnectionInfo"] == "yes") SaveConnectionInfo = true;
                        else SaveConnectionInfo = false;
                    }

                    if (options.ContainsKey("ClickRestoreType"))
                    {
                        if (options["ClickRestoreType"] == "single") RestoreType = ClickRestoreType.SingleClick;
                        else RestoreType = ClickRestoreType.DoubleClick;
                    }

                    if (options.ContainsKey("HighlightTrayForJoinQuits"))
                    {
                        if (options["HighlightTrayForJoinQuits"] == "yes") HighlightTrayIconForJoinsAndQuits = true;
                        else HighlightTrayIconForJoinsAndQuits = false;
                    }

                    if (options.ContainsKey("UsersBack")) UsersBack = App.CreateBrushFromString(options["UsersBack"]);
                    if (options.ContainsKey("UsersFore")) UsersFore = App.CreateBrushFromString(options["UsersFore"]);
                    if (options.ContainsKey("EntryBack")) EntryBack = App.CreateBrushFromString(options["EntryBack"]);
                    if (options.ContainsKey("EntryFore")) EntryFore = App.CreateBrushFromString(options["EntryFore"]);
                    if (options.ContainsKey("ChatBack")) ChatBack = App.CreateBrushFromString(options["ChatBack"]);
                    if (options.ContainsKey("TimeFore")) TimeFore = App.CreateBrushFromString(options["TimeFore"]);
                    if (options.ContainsKey("NickFore")) NickFore = App.CreateBrushFromString(options["NickFore"]);
                    if (options.ContainsKey("BracketFore")) BracketFore = App.CreateBrushFromString(options["BracketFore"]);
                    if (options.ContainsKey("TextFore")) TextFore = App.CreateBrushFromString(options["TextFore"]);
                    if (options.ContainsKey("OwnNickFore")) OwnNickFore = App.CreateBrushFromString(options["OwnNickFore"]);
                    if (options.ContainsKey("LinkFore")) LinkFore = App.CreateBrushFromString(options["LinkFore"]);

                    if (options.ContainsKey("Font"))
                        Font = new FontFamily(options["Font"]);

                    if (options.ContainsKey("TimestampFormat"))
                        TimeStampFormat = options["TimestampFormat"];

                    if (options.ContainsKey("QueryTextFore"))
                        QueryTextFore = App.CreateBrushFromString(options["QueryTextFore"]);

                    if (options.ContainsKey("WindowsForPrivMsgs"))
                    {
                        if (options["WindowsForPrivMsgs"] == "yes") WindowsForPrivMsgs = true;
                        else WindowsForPrivMsgs = false;
                    }

                    if (options.ContainsKey("LastFMUserName"))
                        LastFMUserName = options["LastFMUserName"];
                }
            }
            catch (Exception ex)
            {
                Error.ShowError(new Exception("There was an error reading the configuration file", ex));
            }
        }

        private void SaveConfigurationFile()
        {
            StringBuilder options = new StringBuilder();

            if (SaveConnectionInfo)
            {
                options.AppendLine("FirstChannel:" + FirstChannel);
                options.AppendLine("Nickname:" + InitialNickname);
                options.AppendLine("Server:" + Server);
                options.AppendLine("ServerPort:" + ServerPort);
                options.AppendLine("FirstChannelKey:" + FirstChannelKey);
            }
            options.AppendLine("SaveConnectionInfo:" + ((SaveConnectionInfo == true) ? "yes" : "no"));
            options.AppendLine("ClickRestoreType:" + ((RestoreType == ClickRestoreType.SingleClick) ? "single" : "double"));
            options.AppendLine("HighlightTrayForJoinQuits:" + ((HighlightTrayIconForJoinsAndQuits == true) ? "yes" : "no"));
            options.AppendLine("UsersBack:" + UsersBack.Color.ToString());
            options.AppendLine("UsersFore:" + UsersFore.Color.ToString());
            options.AppendLine("EntryBack:" + EntryBack.Color.ToString());
            options.AppendLine("EntryFore:" + EntryFore.Color.ToString());
            options.AppendLine("ChatBack:" + ChatBack.Color.ToString());
            options.AppendLine("TimeFore:" + TimeFore.Color.ToString());
            options.AppendLine("NickFore:" + NickFore.Color.ToString());
            options.AppendLine("BracketFore:" + BracketFore.Color.ToString());
            options.AppendLine("TextFore:" + TextFore.Color.ToString());
            options.AppendLine("QueryTextFore:" + QueryTextFore.Color.ToString());
            options.AppendLine("OwnNickFore:" + OwnNickFore.Color.ToString());
            options.AppendLine("LinkFore:" + LinkFore.Color.ToString());
            options.AppendLine("Font:" + Font.Source);
            options.AppendLine("TimestampFormat:" + TimeStampFormat);
            options.AppendLine("WindowsForPrivMsgs:" + ((WindowsForPrivMsgs == true) ? "yes" : "no"));
            options.AppendLine("LastFMUserName:" + LastFMUserName);

            File.WriteAllText(App.CONFIG_FILE_NAME, options.ToString());
        }
        #endregion

        private bool ShowConnectionWindow(ChannelWindow FirstWindow)
        {
            ConnectionWindow connWin = new ConnectionWindow(this);
            connWin.Owner = FirstWindow;
            if (connWin.ShowDialog().Value)
            {
                FirstChannel = connWin.Channel;
                InitialNickname = connWin.Nickname;
                Server = connWin.Server;
                FirstChannelKey = connWin.ChannelKey;
            }
            else
            {
                Shutdown();
            }

            return connWin.DialogResult.Value;
        }

        public void ShowOptions()
        {
            Options optionsDialog = new Options(this);
            if (optionsDialog.ShowDialog().Value)
                SaveConfigurationFile();
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        void channelWindow_Closed(object sender, EventArgs e)
        {
            ChannelWindow chan = sender as ChannelWindow;
            if (chan != null) chan.Closed -= channelWindow_Closed;

            // if the closing window was our "main" window, we need to choose a new "main" window
            if (sender == MainOutputWindow && channelWindows.Count > 1)
                foreach (KeyValuePair<string, ChannelWindow> pair in channelWindows)
                    if (pair.Value != MainOutputWindow)
                    {
                        MainOutputWindow = pair.Value;
                        MainOutputWindow.IsMainWindow = true;
                        continue;
                    }

            List<string> keysToRemove = new List<string>();
            foreach (KeyValuePair<string, ChannelWindow> pair in channelWindows)
                if (pair.Value == sender)
                    keysToRemove.Add(pair.Key);
            foreach (string s in keysToRemove)
                channelWindows.Remove(s);

            Window_Closed(sender, e);
        }

        protected void ParseUserInput(ChatWindow sender, string input)
        {
            try
            {
                string target = (sender is ChannelWindow) ? (sender as ChannelWindow).Channel : (sender as PrivMsg).QueriedUser;
                string[] words = input.Split(' ');

                if (input.Equals("/clear", StringComparison.CurrentCultureIgnoreCase))
                    sender.Clear();
                else if (input.Equals("/options", StringComparison.CurrentCultureIgnoreCase))
                {
                    ShowOptions();
                }
                else if (words[0].Equals("/me", StringComparison.CurrentCultureIgnoreCase))
                {
                    string action;
                    if (input.Length >= 5)
                        action = input.Substring(4);
                    else
                        action = "";
                    IRC.SendMessage(SendType.Action, target, action);

                    sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "* "),
                                                        new ColorTextPair(TextFore, IRC.Nickname) },
                                  new ColorTextPair[] { new ColorTextPair(TextFore, action) });
                }
                else if (words[0].Equals("/nick", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (words.Length == 2)
                        IRC.RfcNick(words[1]);
                    else
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/me <newName>'.  Names may not contain spaces.") });
                }
                else if (words[0].Equals("/topic", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (sender is PrivMsg)
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "Cannot set topic in a private chat") });
                    else
                    {
                        if (input.Length >= 8)
                            IRC.RfcTopic(target, input.Substring(7));
                        else
                            sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                          new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/topic <new topic>'.") });
                    }
                }
                else if (words[0].Equals("/raw", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 6)
                        IRC.WriteLine(input.Substring(5));
                    else
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/raw <raw IRC message>'.") });
                }
                else if (words[0].Equals("/msg", StringComparison.CurrentCultureIgnoreCase))
                {
                    bool syntaxError = false;
                    if (words.Length >= 2 && !string.IsNullOrEmpty(words[1]))
                    {
                        string otherTarget = words[1];
                        if (words.Length >= 3)
                        {
                            string msgText = input.Substring(input.IndexOf(" " + words[1] + " ") + words[1].Length + 2);
                            SendQueryMessage(otherTarget, msgText);
                        }
                        else syntaxError = true;
                    }
                    else syntaxError = true;

                    if (syntaxError)
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/msg <name> <message>'.") });
                }
                else if (words[0].Equals("/error", StringComparison.CurrentCultureIgnoreCase))
                {
                    string errorText;
                    if (input.Length >= 8)
                        errorText = input.Substring(7);
                    else
                        errorText = "error";

                    throw new Exception(errorText);
                }
                else if (words[0].Equals("/np", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (string.IsNullOrEmpty(LastFMUserName))
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "You must choose a Last.fm username on the options dialog.") });
                    else
                    {
                        WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
                        client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LastFMdownloadComplete);
                        client.DownloadStringAsync(new Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + LastFMUserName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"), sender);
                    }
                }
                else if (words[0].Equals("/join", StringComparison.CurrentCultureIgnoreCase))
                {
                    bool syntaxError = false;
                    string channel = null;
                    string channelKey = null;
                    if (words.Length >= 2 && !string.IsNullOrEmpty(words[1]))
                    {
                        channel = words[1];
                        if (words.Length == 3)
                            channelKey = words[2];
                        else if (words.Length > 3)
                            syntaxError = true;
                    }
                    else syntaxError = true;

                    if (!syntaxError)
                        IRC.RfcJoin(channel, channelKey);
                    else
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/join <channelName>'.  Names may not contain spaces.") });
                }
                else if (input.StartsWith("/"))
                {
                    sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                  new ColorTextPair[] { new ColorTextPair(TextFore, "command not recognized.") });
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    IRC.SendMessage(SendType.Message, target, input);

                    sender.Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "<"),
                                                        new ColorTextPair(OwnNickFore, IRC.Nickname),
                                                        new ColorTextPair(BracketFore, ">") },
                                  new ColorTextPair[] { new ColorTextPair(TextFore, input) });
                }
            }
            catch (Exception ex)
            {
                Error.ShowError(ex);
            }
        }

        private void LastFMdownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            ChatWindow window = e.UserState as ChatWindow;

            try
            {
                string target = (window is ChannelWindow) ? (window as ChannelWindow).Channel : (window as PrivMsg).QueriedUser;

                XDocument doc = XDocument.Parse(e.Result);

                var tracks = from results in doc.Descendants("track")
                             select new { name = results.Element("name").Value, artist = results.Element("artist").Value, date = (DateTime)results.Element("date") };

                foreach (var track in tracks)
                {
                    if (DateTime.Now.Subtract(track.date).Minutes > 30)
                    {
                        window.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "You haven't submitted a song to Last.fm in the last 30 minutes.  Maybe Last.fm submission service is down?") });
                    }
                    else
                    {
                        string action = "is listening to " + track.name + " by " + track.artist;
                        IRC.SendMessage(SendType.Action, target, action);

                        window.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "* "),
                                                            new ColorTextPair(TextFore, IRC.Nickname) },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, action) });
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                window.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                              new ColorTextPair[] { new ColorTextPair(TextFore, ex.Message) });
            }
        }
    }

    delegate void VoidDelegate();
}
