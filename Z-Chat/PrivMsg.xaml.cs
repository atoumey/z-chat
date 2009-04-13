using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Meebey.SmartIrc4net;
using System.Windows.Threading;
using System.Net;
using System.Xml.Linq;
using System.ComponentModel;

namespace ZChat
{
    public partial class PrivMsg : ChatWindow
    {
        private string QueriedUser;
        
        /// <summary>
        /// Call when the first message is incoming.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="queriedUserName"></param>
        /// <param name="firstMessage">The IrcEventArgs that were received with the incoming message.</param>
        public PrivMsg(App app, string queriedUserName, IrcEventArgs firstMessage)
            : this(app, queriedUserName)
        {
            if (firstMessage.Data.Type == ReceiveType.QueryNotice)
                IRC_OnQueryNotice(this, firstMessage);
            else if (firstMessage.Data.Type == ReceiveType.QueryMessage)
                IRC_OnQueryMessage(this, firstMessage);
            else if (firstMessage.Data.Type == ReceiveType.QueryAction)
                IRC_OnQueryAction(this, (ActionEventArgs)firstMessage);
        }

        /// <summary>
        /// Call when the first message is outgoing.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="queriedUserName"></param>
        /// <param name="firstMessage">The initial message to send to the other user.</param>
        public PrivMsg(App app, string queriedUserName, string firstMessage)
            : this(app, queriedUserName)
        {
            SendMessage(firstMessage);
        }

        protected PrivMsg(App app, string queriedUserName) : base(app)
        {
            WindowIconName_NoActivity = "ZChat.IRC.ico";
            WindowIconName_Activity = "ZChat.IRC.ico";
            TrayIconName_NoActivity = "ZChat.IRCgreen.ico";
            TrayIconName_Activity = "ZChat.IRCgreen.ico";

            ZChat.PropertyChanged += ZChat_PropertyChanged;
            ZChat.IRC.OnQueryAction += new ActionEventHandler(IRC_OnQueryAction);
            ZChat.IRC.OnQueryMessage += new IrcEventHandler(IRC_OnQueryMessage);
            ZChat.IRC.OnQueryNotice += new IrcEventHandler(IRC_OnQueryNotice);
            ZChat.IRC.OnNickChange += new NickChangeEventHandler(IRC_OnNickChange);
            ZChat.IRC.OnQuit += new QuitEventHandler(IRC_OnQuit);

            InitializeComponent();
            QueriedUser = queriedUserName;

            UpdateTitle();
        }

        void IRC_OnQuit(object sender, QuitEventArgs e)
        {
            if (e.Who != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                           new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " quit (" + e.QuitMessage + ")") });

            if (ZChat.HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void IRC_OnQueryNotice(object sender, IrcEventArgs e)
        {
            if (e.Data.Nick != QueriedUser) return;

            ColorTextPair[] source;
            if (e.Data.Nick == null)
                source = new ColorTextPair[] { };
            else
                source = new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, "*"),
                                         new ColorTextPair(ZChat.QueryTextFore, e.Data.Nick),
                                         new ColorTextPair(ZChat.QueryTextFore, "*") };

            Output(source, new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, e.Data.Message) });
            ShowActivity();
        }

        void IRC_OnQueryMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Nick != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "<"),
                                                         new ColorTextPair(ZChat.NickFore, e.Data.Nick),
                                                         new ColorTextPair(ZChat.BracketFore, ">") },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Data.Message) });
            ShowActivity();
        }

        void IRC_OnQueryAction(object sender, ActionEventArgs e)
        {
            if (e.Data.Nick != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "* "),
                                                         new ColorTextPair(ZChat.TextFore, e.Data.Nick) },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.ActionMessage.Substring(1)) });
            ShowActivity();
        }

        void ZChat_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void UpdateTitle()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
            {
                Title = "Private message with " + QueriedUser;
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InputBox = inputTextBox;
            Document = chatFlowDoc;
            DocumentScrollViewer = chatScrollViewer;

            InputBox.Background = ZChat.EntryBack;
            InputBox.Foreground = ZChat.EntryFore;
            Document.Background = ZChat.ChatBack;
        }

        private void ParseUserInput(object sender, string input)
        {
            try
            {
                string[] words = inputTextBox.Text.Split(' ');

                if (input.Equals("/clear", StringComparison.CurrentCultureIgnoreCase))
                    chatFlowDoc.Blocks.Clear();
                else if (words[0].Equals("/me", StringComparison.CurrentCultureIgnoreCase))
                {
                    string action;
                    if (input.Length >= 5)
                        action = input.Substring(4);
                    else
                        action = "";
                    ZChat.IRC.SendMessage(SendType.Action, QueriedUser, action);

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "* "),
                                                     new ColorTextPair(ZChat.TextFore, ZChat.IRC.Nickname) },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, action) });
                    }));
                }
                else if (words[0].Equals("/raw", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 6)
                        ZChat.IRC.WriteLine(input.Substring(5));
                    else
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "! Error:") },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command syntax is '/raw <raw IRC message>'.") });
                        }));
                }
                else if (words[0].Equals("/np", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (string.IsNullOrEmpty(ZChat.LastFMUserName))
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "You must choose a Last.fm username on the options dialog.") });
                        }));
                    else
                    {
                        WebClient client = new WebClient();
                        client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LastFMdownloadComplete);
                        client.DownloadStringAsync(new Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + ZChat.LastFMUserName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"));
                    }
                }
                else if (input.StartsWith("/"))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "! Error:") },
                               new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "command not recognized.") });
                    }));
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    SendMessage(input);
                }
            }
            catch (Exception ex)
            {
                Error.ShowError(ex);
            }
        }

        private void SendMessage(string input)
        {
            ZChat.IRC.SendMessage(SendType.Message, QueriedUser, input);

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "<"),
                                                 new ColorTextPair(ZChat.OwnNickFore, ZChat.IRC.Nickname),
                                                 new ColorTextPair(ZChat.BracketFore, ">") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, input) });
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
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "You haven't submitted a song to Last.fm in the last 30 minutes.  Maybe Last.fm submission service is down?") });
                        }));
                    }
                    else
                    {
                        string action = "is listening to " + track.name + " by " + track.artist;
                        ZChat.IRC.SendMessage(SendType.Action, QueriedUser, action);

                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "* "),
                                                     new ColorTextPair(ZChat.TextFore, ZChat.IRC.Nickname) },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, action) });
                        }));
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                {
                    Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                           new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, ex.Message) });
                }));
            }
        }

        internal void SetChatBackground(SolidColorBrush _chatBack)
        {
            chatFlowDoc.Background = _chatBack;
        }

        internal void SetInputForeground(SolidColorBrush _entryFore)
        {
            inputTextBox.Foreground = _entryFore;
        }

        internal void SetInputBackground(SolidColorBrush _entryBack)
        {
            inputTextBox.Background = _entryBack;
        }

        protected void IRC_OnNickChange(object sender, NickChangeEventArgs e)
        {
            if (e.OldNickname != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.OldNickname + " changed their name to " + e.NewNickname) });

            QueriedUser = e.NewNickname;
            UpdateTitle();

            ShowActivity();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Document = chatFlowDoc;
            DocumentScrollViewer = chatScrollViewer;
            InputBox = inputTextBox;

            UserInput += ParseUserInput;
        }

        public void TakeOutgoingMessage(string message)
        {
            SendMessage(message);
        }
    }
}
