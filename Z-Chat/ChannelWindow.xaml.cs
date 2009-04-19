using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.IO.IsolatedStorage;

using Meebey.SmartIrc4net;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace ZChat
{
    public partial class ChannelWindow : ChatWindow
    {
        public string Channel
        {
            get { return _channel; }
            set
            {
                _channel = value;
                Title = _channel;
                if (IsMainWindow) Title += " *";
                notifyIcon.Text = _channel;
            }
        }
        protected string _channel;
        public string ChannelKey;
        private string topic;

        public bool IsMainWindow
        {
            get { return _isMainWindow; }
            set
            {
                _isMainWindow = value;
                if (_isMainWindow)
                {
                    Title += " *";
                    ZChat.IRC.OnErrorMessage += irc_OnErrorMessage;
                    ZChat.IRC.OnError += irc_OnError;
                    ZChat.IRC.OnConnectionError += irc_OnConnectionError;
                }
                else
                {
                    ZChat.IRC.OnErrorMessage -= irc_OnErrorMessage;
                    ZChat.IRC.OnError -= irc_OnError;
                    ZChat.IRC.OnConnectionError -= irc_OnConnectionError;
                }
            }
        }
        private bool _isMainWindow = false;

        public ChannelWindow() { }

        public ChannelWindow(App app) : base(app)
        {
            WindowIconName_NoActivity = "ZChat.IRC.ico";
            WindowIconName_Activity = "ZChat.IRCgreen.ico";
            TrayIconName_NoActivity = "ZChat.IRC.ico";
            TrayIconName_Activity = "ZChat.IRCgreen.ico";

            ZChat.PropertyChanged += ZChat_PropertyChanged;

            InitializeComponent();

            ZChat.IRC.OnPart += new PartEventHandler(irc_OnPart);
            ZChat.IRC.OnQuit += new QuitEventHandler(irc_OnQuit);
            ZChat.IRC.OnChannelMessage += new IrcEventHandler(irc_OnChannelMessage);
            ZChat.IRC.OnJoin += new JoinEventHandler(irc_OnJoin);
            ZChat.IRC.OnChannelActiveSynced += new IrcEventHandler(irc_OnChannelActiveSynced);
            ZChat.IRC.OnConnected += new EventHandler(irc_OnConnected);
            ZChat.IRC.OnDisconnected += new EventHandler(irc_OnDisconnected);
            ZChat.IRC.OnKick += new KickEventHandler(irc_OnKick);
            ZChat.IRC.OnNickChange += new NickChangeEventHandler(irc_OnNickChange);
            ZChat.IRC.OnChannelAction += new ActionEventHandler(irc_OnChannelAction);
            ZChat.IRC.OnTopic += new TopicEventHandler(irc_OnTopic);
            ZChat.IRC.OnTopicChange += new TopicChangeEventHandler(irc_OnTopicChange);
        }

        void ZChat_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UsersBack")
                usersListBox.Background = ZChat.UsersBack;
            if (e.PropertyName == "UsersFore")
                usersListBox.Foreground = ZChat.UsersFore;
            if (e.PropertyName == "Font")
            {
                usersListBox.FontFamily = ZChat.Font;
                topicTextBox.Document.FontFamily = ZChat.Font;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            usersListBox.Background = ZChat.UsersBack;
            usersListBox.Foreground = ZChat.UsersFore;

            usersListBox.FontFamily = ZChat.Font;
            topicTextBox.Document.FontFamily = ZChat.Font;
        }

        void irc_OnTopicChange(object sender, TopicChangeEventArgs e)
        {
            if (e.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " changed the topic to: " + e.NewTopic) });

            UpdateTopic(e.NewTopic);
        }

        private void UpdateTopic(string newTopic)
        {
            topic = newTopic;

            Dispatcher.Invoke(new VoidDelegate(delegate
            {
                Paragraph p = new Paragraph();
                p.TextAlignment = TextAlignment.Left;

                AddInlines(p.Inlines, new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, newTopic) }, true);

                topicTextBox.Document.Blocks.Clear();
                topicTextBox.Document.Blocks.Add(p);
            }));
        }

        void irc_OnTopic(object sender, TopicEventArgs e)
        {
            if (e.Channel != Channel) return;

            UpdateTopic(e.Topic);
        }

        private string lastQuerySender = null;

        void irc_OnQueryNotice(object sender, IrcEventArgs e)
        {
            ColorTextPair[] source;
            if (e.Data.Nick == null)
                source = new ColorTextPair[] {};
            else
                source = new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, "*"),
                                         new ColorTextPair(ZChat.QueryTextFore, e.Data.Nick),
                                         new ColorTextPair(ZChat.QueryTextFore, "*") };

            Output(source, new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, e.Data.Message) });

            lastQuerySender = e.Data.Nick;
            ShowActivity();
        }

        void irc_OnQueryAction(object sender, ActionEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, "* "),
                                         new ColorTextPair(ZChat.QueryTextFore, e.Data.Nick) },
                   new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, e.ActionMessage.Substring(1)) });

            lastQuerySender = e.Data.Nick;
            ShowActivity();
        }

        void irc_OnQueryMessage(object sender, IrcEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, "*"),
                                     new ColorTextPair(ZChat.QueryTextFore, e.Data.Nick),
                                     new ColorTextPair(ZChat.QueryTextFore, "*") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, e.Data.Message) });

            lastQuerySender = e.Data.Nick;
            ShowActivity();
        }

        void irc_OnChannelAction(object sender, ActionEventArgs e)
        {
            if (e.Data.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "* "),
                                         new ColorTextPair(ZChat.TextFore, e.Data.Nick) },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.ActionMessage.Substring(1)) });

            ShowActivity();
        }

        void irc_OnNickChange(object sender, NickChangeEventArgs e)
        {
            if (!Users.Contains(e.OldNickname)) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.OldNickname + " changed their name to " + e.NewNickname) });

            UpdateUsers();
            ShowActivity();
        }

        void irc_OnKick(object sender, KickEventArgs e)
        {
            if (e.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " kicked " + e.Whom) });

            UpdateUsers();
            if (ZChat.HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnErrorMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message;
            if (e.Data.ReplyCode == ReplyCode.ErrorNoSuchChannel ||
                e.Data.ReplyCode == ReplyCode.ErrorNicknameInUse)
                message += ": " + e.Data.RawMessageArray[3];

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, message) });

            UpdateUsers();
        }

        void irc_OnError(object sender, Meebey.SmartIrc4net.ErrorEventArgs e)
        {
            Error.ShowError(new Exception("SmartIrc4Net error: " + e.ErrorMessage));
        }

        void irc_OnDisconnected(object sender, EventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "you disconnected") });

            UpdateUsers();
            ShowActivity();
        }

        void irc_OnConnectionError(object sender, EventArgs e)
        {
            UpdateUsers();
        }

        void irc_OnPart(object sender, PartEventArgs e)
        {
            if (e.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " left the chat (" + e.PartMessage + ")") });

            UpdateUsers();
            if (ZChat.HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnQuit(object sender, QuitEventArgs e)
        {
            if (!Users.Contains(e.Who)) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " quit (" + e.QuitMessage + ")") });

            UpdateUsers();
            if (ZChat.HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Channel != Channel) return;

            NonRfcChannelUser user = (NonRfcChannelUser)ZChat.IRC.GetChannelUser(Channel, e.Data.Nick);
            string userName;
            if (user.IsOp) userName = '@' + user.Nick;
            else if (user.IsHalfop) userName = '%' + user.Nick;
            else if (user.IsVoice) userName = '+' + user.Nick;
            else userName = user.Nick;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "<"),
                                         new ColorTextPair(ZChat.NickFore, userName),
                                         new ColorTextPair(ZChat.BracketFore, ">") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Data.Message) });

            ShowActivity();
        }

        void irc_OnConnected(object sender, EventArgs e)
        {
            ZChat.IRC.RfcJoin(Channel, ChannelKey);
        }

        void irc_OnChannelActiveSynced(object sender, IrcEventArgs e)
        {
            UpdateUsers();
        }

        void UpdateUsers()
        {
            Channel chan = ZChat.IRC.GetChannel(Channel);
            
            Users.Clear();

            List<string> ops = new List<string>();
            List<string> halfops = new List<string>();
            List<string> voices = new List<string>();
            List<string> normals = new List<string>();
            if (chan != null)
                foreach (NonRfcChannelUser user in chan.Users.Values)
                {
                    if (user.IsOp) ops.Add('@' + user.Nick);
                    else if (user.IsHalfop) halfops.Add('%' + user.Nick);
                    else if (user.IsVoice) voices.Add('+' + user.Nick);
                    else normals.Add(user.Nick);
                }

            StringComparer comparer = StringComparer.Create(CultureInfo.CurrentCulture, true);
            foreach (List<string> list in new List<string>[] {ops, halfops, voices, normals})
            {
                list.Sort(comparer);
                Users.AddRange(list);
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(delegate
            {
                usersListBox.Items.Clear();
                foreach (string user in Users) usersListBox.Items.Add(user);
            }));
        }

        void irc_OnJoin(object sender, JoinEventArgs e)
        {
            if (e.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " joined the chat") });

            UpdateUsers();
            if (ZChat.HighlightTrayIconForJoinsAndQuits)
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
                    ZChat.ShowOptions();
                }
                else if (words[0].Equals("/me", StringComparison.CurrentCultureIgnoreCase))
                {
                    string action;
                    if (input.Length >= 5)
                        action = input.Substring(4);
                    else
                        action = "";
                    ZChat.IRC.SendMessage(SendType.Action, Channel, action);

                    Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "* "),
                                                 new ColorTextPair(ZChat.TextFore, ZChat.IRC.Nickname) },
                           new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, action) });
                }
                else if (words[0].Equals("/nick", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (words.Length == 2)
                        ZChat.IRC.RfcNick(words[1]);
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command syntax is '/me <newName>'.  Names may not contain spaces.") });
                }
                else if (words[0].Equals("/topic", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 8)
                        ZChat.IRC.RfcTopic(Channel, input.Substring(7));
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command syntax is '/topic <new topic>'.") });
                }
                else if (words[0].Equals("/raw", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 6)
                        ZChat.IRC.WriteLine(input.Substring(5));
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command syntax is '/raw <raw IRC message>'.") });
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
                            ZChat.SendQueryMessage(target, msgText);
                        }
                        else syntaxError = true;
                    }
                    else syntaxError = true;

                    if (syntaxError)
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command syntax is '/msg <name> <message>'.") });
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
                    if (string.IsNullOrEmpty(ZChat.LastFMUserName))
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "You must choose a Last.fm username on the options dialog.") });
                    else
                    {
                        WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
                        client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LastFMdownloadComplete);
                        client.DownloadStringAsync(new Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + ZChat.LastFMUserName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"));
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
                        ZChat.IRC.RfcJoin(channel, channelKey);
                    else
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command syntax is '/join <channelName>'.  Names may not contain spaces.") });
                }
                else if (input.StartsWith("/"))
                {
                    Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "   Error:") },
                           new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command not recognized.") });
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    ZChat.IRC.SendMessage(SendType.Message, Channel, inputTextBox.Text);

                    Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "<"),
                                                 new ColorTextPair(ZChat.OwnNickFore, ZChat.IRC.Nickname),
                                                 new ColorTextPair(ZChat.BracketFore, ">") },
                           new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, inputTextBox.Text) });
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
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "You haven't submitted a song to Last.fm in the last 30 minutes.  Maybe Last.fm submission service is down?") });
                    }
                    else
                    {
                        string action = "is listening to " + track.name + " by " + track.artist;
                        ZChat.IRC.SendMessage(SendType.Action, Channel, action);

                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "* "),
                                                 new ColorTextPair(ZChat.TextFore, ZChat.IRC.Nickname) },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, action) });
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                       new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, ex.Message) });
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

        private void Window_Initialized(object sender, EventArgs e)
        {
            Document = chatFlowDoc;
            DocumentScrollViewer = chatScrollViewer;
            InputBox = inputTextBox;

            UserInput += ParseUserInput;
        }

        public void TakeIncomingQueryMessage(IrcEventArgs e)
        {
            if (e.Data.Type == ReceiveType.QueryNotice)
                irc_OnQueryNotice(this, e);
            else if (e.Data.Type == ReceiveType.QueryMessage)
                irc_OnQueryMessage(this, e);
            else if (e.Data.Type == ReceiveType.QueryAction)
                irc_OnQueryAction(this, (ActionEventArgs)e);
        }

        public void TakeOutgoingQueryMessage(string nick, string message)
        {
            ZChat.IRC.SendMessage(SendType.Message, nick, message);

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, "->*" + nick + "*") },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, message) });
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ZChat.IRC.RfcPart(Channel);

            ZChat.PropertyChanged -= ZChat_PropertyChanged;

            ZChat.IRC.OnPart -= irc_OnPart;
            ZChat.IRC.OnQuit -= irc_OnQuit;
            ZChat.IRC.OnChannelMessage -= irc_OnChannelMessage;
            ZChat.IRC.OnJoin -= irc_OnJoin;
            ZChat.IRC.OnChannelActiveSynced -= irc_OnChannelActiveSynced;
            ZChat.IRC.OnConnected -= irc_OnConnected;
            ZChat.IRC.OnConnectionError -= irc_OnConnectionError;
            ZChat.IRC.OnDisconnected -= irc_OnDisconnected;
            ZChat.IRC.OnError -= irc_OnError;
            ZChat.IRC.OnErrorMessage -= irc_OnErrorMessage;
            ZChat.IRC.OnKick -= irc_OnKick;
            ZChat.IRC.OnNickChange -= irc_OnNickChange;
            ZChat.IRC.OnChannelAction -= irc_OnChannelAction;
            ZChat.IRC.OnTopic -= irc_OnTopic;
            ZChat.IRC.OnTopicChange -= irc_OnTopicChange;
        }
    }
}
