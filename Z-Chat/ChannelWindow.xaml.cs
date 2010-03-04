using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Meebey.SmartIrc4net;

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
                    ZChat.IRC.OnInvite += new InviteEventHandler(IRC_OnInvite);
                }
                else
                {
                    ZChat.IRC.OnErrorMessage -= irc_OnErrorMessage;
                    ZChat.IRC.OnError -= irc_OnError;
                    ZChat.IRC.OnConnectionError -= irc_OnConnectionError;
                    ZChat.IRC.OnInvite -= new InviteEventHandler(IRC_OnInvite);
                }
            }
        }
        private bool _isMainWindow = false;

        public ChannelWindow() { }

        public ChannelWindow(Chat zchat) : base(zchat)
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
            ZChat.IRC.OnChannelModeChange += new IrcEventHandler(IRC_OnChannelModeChange);
            ZChat.IRC.OnChannelNotice += new IrcEventHandler(IRC_OnChannelNotice);
            ZChat.IRC.OnNames += new NamesEventHandler(IRC_OnNames);
            ZChat.IRC.OnNowAway += new IrcEventHandler(IRC_SelfAwayOrUnaway);
            ZChat.IRC.OnUnAway += new IrcEventHandler(IRC_SelfAwayOrUnaway);
            ZChat.IRC.OnAway += new AwayEventHandler(IRC_OnAway);
        }

        void IRC_SelfAwayOrUnaway(object sender, IrcEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Data.Message) });
        }

        void IRC_OnNames(object sender, NamesEventArgs e)
        {
            if (e.Channel != Channel) return;
            UpdateUsers();
        }

        void IRC_OnInvite(object sender, InviteEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " invited you to " + e.Channel) });

            ShowActivity();
        }

        void IRC_OnChannelNotice(object sender, IrcEventArgs e)
        {
            if (e.Data.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "-"),
                                         new ColorTextPair(ZChat.NickFore, ZChat.IRC.Nickname),
                                         new ColorTextPair(ZChat.BracketFore, "-") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Data.Message) });

            ShowActivity();
        }

        void IRC_OnChannelModeChange(object sender, IrcEventArgs e)
        {
            if (e.Data.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Data.RawMessage.Substring(e.Data.RawMessage.IndexOf(" mode ", StringComparison.CurrentCultureIgnoreCase) + 1) + " by " + e.Data.Nick) });

            UpdateUsers();
        }

        void IRC_OnAway(object sender, AwayEventArgs e)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " is away (" + e.AwayMessage + ")") });
        }

        void ZChat_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UsersBack")
                usersListBox.Background = ZChat.UsersBack;
            if (e.PropertyName == "UsersFore")
                usersListBox.Foreground = ZChat.UsersFore;
            if (e.PropertyName == "ChatBack")
                topicTextBox.Background = ZChat.ChatBack;
            if (e.PropertyName == "TextFore")
                RefreshTopicForeColors();
            if (e.PropertyName == "LinkFore")
                RefreshTopicForeColors();
            if (e.PropertyName == "Font")
            {
                usersListBox.FontFamily = ZChat.Font;
                topicTextBox.Document.FontFamily = ZChat.Font;
            }
        }

        private void RefreshTopicForeColors()
        {
            foreach (Paragraph p in topicTextBox.Document.Blocks)
            {
                foreach (Inline inline in p.Inlines)
                {
                    if (inline is Run)
                        inline.Foreground = ZChat.TextFore;
                    if (inline is Hyperlink)
                        inline.Foreground = ZChat.LinkFore;
                }
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
                source = new ColorTextPair[] { new ColorTextPair(ZChat.QueryTextFore, "-"),
                                         new ColorTextPair(ZChat.QueryTextFore, e.Data.Nick),
                                         new ColorTextPair(ZChat.QueryTextFore, "-") };

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
            if (!UsersContains(e.OldNickname)) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.OldNickname + " changed their name to " + e.NewNickname) });

            UpdateUsers();
            ShowActivity();
        }

        void irc_OnKick(object sender, KickEventArgs e)
        {
            if (e.Channel != Channel) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " kicked " + e.Whom + "(" + e.KickReason + ")") });

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
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " left the channel (" + e.PartMessage + ")") });

            UpdateUsers();
            if (ZChat.HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void irc_OnQuit(object sender, QuitEventArgs e)
        {
            if (!UsersContains(e.Who)) return;

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
            ZChat.IRC.OnDisconnected -= irc_OnDisconnected;
            ZChat.IRC.OnKick -= irc_OnKick;
            ZChat.IRC.OnNickChange -= irc_OnNickChange;
            ZChat.IRC.OnChannelAction -= irc_OnChannelAction;
            ZChat.IRC.OnTopic -= irc_OnTopic;
            ZChat.IRC.OnTopicChange -= irc_OnTopicChange;

            if (IsMainWindow)
            {
                ZChat.IRC.OnErrorMessage -= irc_OnErrorMessage;
                ZChat.IRC.OnError -= irc_OnError;
                ZChat.IRC.OnConnectionError -= irc_OnConnectionError;
                ZChat.IRC.OnInvite -= new InviteEventHandler(IRC_OnInvite);
            }
        }
    }
}
