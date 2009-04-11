using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.IO.IsolatedStorage;

using Meebey.SmartIrc4net;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using System.Linq;

namespace ZChat
{
    delegate void VoidDelegate();

    public partial class ChannelWindow : ChatWindow
    {
        public static string ISOLATED_FILE_NAME = "ChatConfig.txt";

        private string server = "irc.mibbit.com";
        private string channel = "#test";
        private string topic;
        private string nick = System.Environment.UserName;
        
        public SolidColorBrush UsersBack { get { return _usersBack; } set { _usersBack = value; usersListBox.Background = value; } }
        private SolidColorBrush _usersBack = Brushes.White;
        public SolidColorBrush UsersFore { get { return _usersFore; } set { _usersFore = value; usersListBox.Foreground = value; } }
        private SolidColorBrush _usersFore = Brushes.Black;

        public bool HighlightTrayIconForJoinsAndQuits = true;

        public new FontFamily Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                usersListBox.FontFamily = base.Font;
                topicTextBox.FontFamily = base.Font;
            }
        }

        public bool WindowsForPrivMsgs = false;
        public string LastFMUserName = "";

        private string[] commandLineArgs;

        private IrcClient irc;

        private Dictionary<string, PrivMsg> queryWindows = new Dictionary<string, PrivMsg>();

        public ChannelWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (irc.IsConnected)
                irc.Disconnect();

            App.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfigurationFile();
            
            usersListBox.FontFamily = Font;
            topicTextBox.Document.FontFamily = Font;

            commandLineArgs = System.Environment.GetCommandLineArgs();

            irc = new IrcClient();
            irc.ActiveChannelSyncing = true;
            irc.AutoNickHandling = true;
            irc.SupportNonRfc = true;
            irc.Encoding = System.Text.Encoding.UTF8;

            irc.OnPart += new PartEventHandler(irc_OnPart);
            irc.OnQuit += new QuitEventHandler(irc_OnQuit);
            irc.OnChannelMessage += new IrcEventHandler(irc_OnChannelMessage);
            irc.OnJoin += new JoinEventHandler(irc_OnJoin);
            irc.OnChannelActiveSynced += new IrcEventHandler(irc_OnChannelActiveSynced);
            irc.OnConnected += new EventHandler(irc_OnConnected);
            irc.OnConnectionError += new EventHandler(irc_OnConnectionError);
            irc.OnDisconnected += new EventHandler(irc_OnDisconnected);
            irc.OnError += new Meebey.SmartIrc4net.ErrorEventHandler(irc_OnError);
            irc.OnErrorMessage += new IrcEventHandler(irc_OnErrorMessage);
            irc.OnKick += new KickEventHandler(irc_OnKick);
            irc.OnNickChange += new NickChangeEventHandler(irc_OnNickChange);
            irc.OnChannelAction += new ActionEventHandler(irc_OnChannelAction);
            irc.OnQueryMessage += new IrcEventHandler(irc_OnQueryMessage);
            irc.OnQueryAction += new ActionEventHandler(irc_OnQueryAction);
            irc.OnQueryNotice += new IrcEventHandler(irc_OnQueryNotice);
            irc.OnTopic += new TopicEventHandler(irc_OnTopic);
            irc.OnTopicChange += new TopicChangeEventHandler(irc_OnTopicChange);

            if (commandLineArgs.Length > 3)
                server = commandLineArgs[3];
            irc.Connect(server, 6667);

            inputTextBox.Focus();
        }

        void irc_OnTopicChange(object sender, TopicChangeEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Who + " changed the topic to: " + e.NewTopic) });

            UpdateTopic(e.NewTopic);
        }

        private void UpdateTopic(string newTopic)
        {
            topic = newTopic;

            Dispatcher.Invoke(new VoidDelegate(delegate
            {
                Paragraph p = new Paragraph();
                p.TextAlignment = TextAlignment.Left;

                AddInlines(p.Inlines, new ColorTextPair[] { new ColorTextPair(TextFore, newTopic) }, true);

                topicTextBox.Document.Blocks.Clear();
                topicTextBox.Document.Blocks.Add(p);
            }));
        }

        void irc_OnTopic(object sender, TopicEventArgs e)
        {
            UpdateTopic(e.Topic);
        }

        private string lastQuerySender = null;

        void irc_OnQueryNotice(object sender, IrcEventArgs e)
        {
            ColorTextPair[] source;
            if (e.Data.Nick == null)
                source = new ColorTextPair[] {};
            else
                source = new ColorTextPair[] { new ColorTextPair(QueryTextFore, "*"),
                                         new ColorTextPair(QueryTextFore, e.Data.Nick),
                                         new ColorTextPair(QueryTextFore, "*") };

            Output(source, new ColorTextPair[] { new ColorTextPair(QueryTextFore, e.Data.Message) });

            lastQuerySender = e.Data.Nick;
            ShowActivity();
        }

        void irc_OnQueryAction(object sender, ActionEventArgs e)
        {
            if (WindowsForPrivMsgs && queryWindows.Count <= 10)
            {
                PrivMsg queryWindow = null;
                foreach (KeyValuePair<string, PrivMsg> pair in queryWindows)
                    if (pair.Key == e.Data.Nick)
                        queryWindow = pair.Value;

                if (queryWindow == null)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        queryWindow = new PrivMsg(this, irc, e.Data.Nick);
                        queryWindow.Show();
                    }));
                    queryWindows.Add(e.Data.Nick, queryWindow);
                }

                queryWindow.Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "* "),
                                                         new ColorTextPair(TextFore, e.Data.Nick) },
                                   new ColorTextPair[] { new ColorTextPair(TextFore, e.ActionMessage.Substring(1)) });

                queryWindow.ShowActivity();
            }
            else
            {
                Output(new ColorTextPair[] { new ColorTextPair(QueryTextFore, "* "),
                                             new ColorTextPair(QueryTextFore, e.Data.Nick) },
                       new ColorTextPair[] { new ColorTextPair(QueryTextFore, e.ActionMessage.Substring(1)) });

                ShowActivity();
            }

            lastQuerySender = e.Data.Nick;
        }

        void irc_OnQueryMessage(object sender, IrcEventArgs e)
        {
            if (WindowsForPrivMsgs && queryWindows.Count <= 10)
            {
                PrivMsg queryWindow = null;
                foreach (KeyValuePair<string, PrivMsg> pair in queryWindows)
                    if (pair.Key == e.Data.Nick)
                        queryWindow = pair.Value;

                if (queryWindow == null)
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        queryWindow = new PrivMsg(this, irc, e.Data.Nick);
                        queryWindow.Show();
                    }));
                    queryWindows.Add(e.Data.Nick, queryWindow);
                }

                queryWindow.Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "<"),
                                                         new ColorTextPair(NickFore, e.Data.Nick),
                                                         new ColorTextPair(BracketFore, ">") },
                                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Data.Message) });

                queryWindow.ShowActivity();
            }
            else
            {
                Output(new ColorTextPair[] { new ColorTextPair(QueryTextFore, "*"),
                                         new ColorTextPair(QueryTextFore, e.Data.Nick),
                                         new ColorTextPair(QueryTextFore, "*") },
                       new ColorTextPair[] { new ColorTextPair(QueryTextFore, e.Data.Message) });

                ShowActivity();
            }

            lastQuerySender = e.Data.Nick;
        }

        private void LoadConfigurationFile()
        {
            try
            {
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User
                    | IsolatedStorageScope.Assembly, null, null);

                string[] fileNames = isoStore.GetFileNames(ISOLATED_FILE_NAME);

                foreach (string file in fileNames)
                    if (file == ISOLATED_FILE_NAME)
                    {
                        IsolatedStorageFileStream iStream = new IsolatedStorageFileStream(ISOLATED_FILE_NAME,
                            FileMode.Open, isoStore);

                        StreamReader reader = new StreamReader(iStream);
                        String line;
                        Dictionary<string, string> options = new Dictionary<string, string>();
                        while ((line = reader.ReadLine()) != null)
                        {
                            int colonPlace = line.IndexOf(':');
                            string optionName = line.Substring(0, colonPlace);
                            options.Add(optionName, line.Substring(colonPlace + 1, line.Length - colonPlace - 1));
                        }

                        reader.Close();

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

                        isoStore.Close();
                    }
            }
            catch (Exception ex)
            {
                Error.ShowError(new Exception("There was an error reading the configuration file from IsolatedStorage", ex));
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User
                    | IsolatedStorageScope.Assembly, null, null);
                
                string[] fileNames = isoStore.GetFileNames(ISOLATED_FILE_NAME);
                foreach (string file in fileNames)
                    if (file == ISOLATED_FILE_NAME)
                        isoStore.DeleteFile(ISOLATED_FILE_NAME);

                isoStore.Close();
            }
        }

        void irc_OnChannelAction(object sender, ActionEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "* "),
                                         new ColorTextPair(TextFore, e.Data.Nick) },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.ActionMessage.Substring(1)) });

            ShowActivity();
        }

        void irc_OnNickChange(object sender, NickChangeEventArgs e)
        {
            List<string> keysToRemove = new List<string>();
            List<KeyValuePair<string, PrivMsg>> newPrivs = new List<KeyValuePair<string, PrivMsg>>();
            foreach (KeyValuePair<string, PrivMsg> pair in queryWindows)
                if (pair.Key == e.OldNickname)
                {
                    pair.Value.NickChange(e.NewNickname);
                    keysToRemove.Add(pair.Key);
                    newPrivs.Add(new KeyValuePair<string,PrivMsg>(e.NewNickname, pair.Value));
                }
            foreach (string s in keysToRemove)
                queryWindows.Remove(s);
            foreach (KeyValuePair<string, PrivMsg> pair in newPrivs)
                queryWindows.Add(pair.Key, pair.Value);

            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.OldNickname + " changed their name to " + e.NewNickname) });

            UpdateUsers();
            ShowActivity();
        }

        void irc_OnKick(object sender, KickEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Who + " kicked " + e.Whom) });

            UpdateUsers();
            if (HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnErrorMessage(object sender, IrcEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Data.Message) });

            UpdateUsers();
        }

        void irc_OnError(object sender, Meebey.SmartIrc4net.ErrorEventArgs e)
        {
            Error.ShowError(new Exception("SmartIrc4Net error: " + e.ErrorMessage));
        }

        void irc_OnDisconnected(object sender, EventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, "you disconnected") });

            UpdateUsers();
            ShowActivity();
        }

        void irc_OnConnectionError(object sender, EventArgs e)
        {
            UpdateUsers();
        }

        void irc_OnPart(object sender, PartEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Who + " left the chat (" + e.PartMessage + ")") });

            UpdateUsers();
            if (HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnQuit(object sender, QuitEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Who + " left the chat (" + e.QuitMessage + ")") });

            PrivMsg win = null;
            foreach (KeyValuePair<string, PrivMsg> privWin in queryWindows)
                if (privWin.Key == e.Who)
                    win = privWin.Value;

            if (win != null)
            {
                win.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                           new ColorTextPair[] { new ColorTextPair(TextFore, e.Who + " left the chat (" + e.QuitMessage + ")") });
                if (HighlightTrayIconForJoinsAndQuits)
                    win.ShowActivity();
            }

            UpdateUsers();
            if (HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "<"),
                                         new ColorTextPair(NickFore, e.Data.Nick),
                                         new ColorTextPair(BracketFore, ">") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Data.Message) });

            ShowActivity();
        }

        void irc_OnConnected(object sender, EventArgs e)
        {
            if (commandLineArgs.Length > 1)
                nick = commandLineArgs[1];
            irc.Login(nick, "Real Name", 0, "username");
            
            new Thread(new ThreadStart(delegate { irc.Listen(); })).Start();

            if (commandLineArgs.Length > 2)
                channel = "#" + commandLineArgs[2];
            irc.RfcJoin(channel);

        }

        void irc_OnChannelActiveSynced(object sender, IrcEventArgs e)
        {
            UpdateUsers();
        }

        void UpdateUsers()
        {
            Channel chan = irc.GetChannel(channel);
            IEnumerable userList;
            if (chan == null)
                userList = new ArrayList();
            else
                userList = chan.Users.Keys;

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
            {
                usersListBox.ItemsSource = userList;
                Users.Clear();
                foreach (string user in userList) Users.Add(user);
            }));
        }

        void irc_OnJoin(object sender, JoinEventArgs e)
        {
            nick = irc.Nickname;

            Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(TextFore, e.Who + " joined the chat") });

            UpdateUsers();
            if (HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        private void inputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.R || e.SystemKey == Key.R) && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
                                      || Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                if (string.IsNullOrEmpty(inputTextBox.Text) && !string.IsNullOrEmpty(lastQuerySender))
                {
                    inputTextBox.Text = "/msg " + lastQuerySender + " ";
                    inputTextBox.CaretIndex = inputTextBox.Text.Length;
                }
            }
        }

        private void ParseUserInput(object sender, string input)
        {
            try
            {
                string[] words = inputTextBox.Text.Split(' ');

                if (input.Equals("/clear", StringComparison.CurrentCultureIgnoreCase))
                    chatFlowDoc.Blocks.Clear();
                else if (input.Equals("/options", StringComparison.CurrentCultureIgnoreCase))
                {
                    Options optionsDialog = new Options(this);
                    optionsDialog.Owner = this;
                    optionsDialog.ShowDialog();
                }
                else if (words[0].Equals("/me", StringComparison.CurrentCultureIgnoreCase))
                {
                    string action;
                    if (input.Length >= 5)
                        action = input.Substring(4);
                    else
                        action = "";
                    irc.SendMessage(SendType.Action, channel, action);

                    Output(new ColorTextPair[] { new ColorTextPair(TextFore, "* "),
                                                 new ColorTextPair(TextFore, irc.Nickname) },
                           new ColorTextPair[] { new ColorTextPair(TextFore, action) });
                }
                else if (words[0].Equals("/nick", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (words.Length == 2)
                        irc.RfcNick(words[1]);
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/me <newName>'.  Names may not contain spaces.") });
                }
                else if (words[0].Equals("/topic", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 8)
                        irc.RfcTopic(channel, input.Substring(7));
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/topic <new topic>'.") });
                }
                else if (words[0].Equals("/raw", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 6)
                        irc.WriteLine(input.Substring(5));
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/raw <raw IRC message>'.") });
                }
                else if (words[0].Equals("/msg", StringComparison.CurrentCultureIgnoreCase))
                {
                    bool syntaxError = false;
                    if (words.Length >= 2 && !string.IsNullOrEmpty(words[1]))
                    {
                        string target = words[1];
                        if (words.Length >= 3)
                        {
                            string msgText = input.Substring(input.IndexOf(" " + words[1] + " ") + words[1].Length + 2);
                            irc.RfcPrivmsg(target, msgText);
                            Output(new ColorTextPair[] { new ColorTextPair(QueryTextFore, "->*" + target + "*") },
                                   new ColorTextPair[] { new ColorTextPair(QueryTextFore, msgText) });
                        }
                        else syntaxError = true;
                    }
                    else syntaxError = true;

                    if (syntaxError)
                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
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
                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "You must choose a Last.fm username on the options dialog.") });
                    else
                    {
                        WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
                        client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LastFMdownloadComplete);
                        client.DownloadStringAsync(new Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + LastFMUserName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"));
                    }
                }
                else if (input.StartsWith("/"))
                {
                    Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                           new ColorTextPair[] { new ColorTextPair(TextFore, "command not recognized.") });
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    irc.SendMessage(SendType.Message, channel, inputTextBox.Text);

                    Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "<"),
                                                 new ColorTextPair(OwnNickFore, irc.Nickname),
                                                 new ColorTextPair(BracketFore, ">") },
                           new ColorTextPair[] { new ColorTextPair(TextFore, inputTextBox.Text) });
                }
            }
            catch (Exception ex)
            {
                Error.ShowError(ex);
            }
        }

        private void LastFMdownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                XDocument doc = XDocument.Parse(e.Result);

                var tracks = from results in doc.Descendants("track")
                             select new { name = results.Element("name").Value, artist = results.Element("artist").Value, date = (DateTime)results.Element("date") };

                foreach (var track in tracks)
                {
                    if (DateTime.Now.Subtract(track.date).Minutes > 30)
                    {
                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "You haven't submitted a song to Last.fm in the last 30 minutes.  Maybe Last.fm submission service is down?") });
                    }
                    else
                    {
                        string action = "is listening to " + track.name + " by " + track.artist;
                        irc.SendMessage(SendType.Action, channel, action);

                        Output(new ColorTextPair[] { new ColorTextPair(TextFore, "* "),
                                                 new ColorTextPair(TextFore, irc.Nickname) },
                               new ColorTextPair[] { new ColorTextPair(TextFore, action) });
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                       new ColorTextPair[] { new ColorTextPair(TextFore, ex.Message) });
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.T && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                if (topicTextBox.Visibility != Visibility.Visible)
                    topicTextBox.Visibility = Visibility.Visible;
                else
                    topicTextBox.Visibility = Visibility.Collapsed;
            }
        }

        public void PrivWindowDied(string queriedUser)
        {
            if (queryWindows.ContainsKey(queriedUser))
                queryWindows.Remove(queriedUser);
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Document = chatFlowDoc;
            DocumentScrollViewer = chatScrollViewer;
            InputBox = inputTextBox;

            UserInput += ParseUserInput;
        }
    }
}
