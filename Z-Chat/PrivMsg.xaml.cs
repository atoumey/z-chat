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
        public string QueriedUser;
        
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
            WindowIconName_Activity = "ZChat.IRCgreen.ico";
            TrayIconName_NoActivity = "ZChat.IRC.ico";
            TrayIconName_Activity = "ZChat.IRCgreen.ico";

            ZChat.PropertyChanged += ZChat_PropertyChanged;
            ZChat.IRC.OnQueryAction += new ActionEventHandler(IRC_OnQueryAction);
            ZChat.IRC.OnQueryMessage += new IrcEventHandler(IRC_OnQueryMessage);
            ZChat.IRC.OnQueryNotice += new IrcEventHandler(IRC_OnQueryNotice);
            ZChat.IRC.OnNickChange += new NickChangeEventHandler(IRC_OnNickChange);
            ZChat.IRC.OnQuit += new QuitEventHandler(IRC_OnQuit);
            ZChat.IRC.OnErrorMessage += new IrcEventHandler(IRC_OnErrorMessage);
            ZChat.IRC.OnAway += new AwayEventHandler(IRC_OnAway);

            InitializeComponent();
            QueriedUser = queriedUserName;

            UpdateTitle();
        }

        void IRC_OnAway(object sender, AwayEventArgs e)
        {
            if (e.Who != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, e.Who + " is now away (" + e.AwayMessage + ")") });
        }

        void IRC_OnErrorMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message;
            if (e.Data.ReplyCode == ReplyCode.ErrorNicknameInUse && e.Data.RawMessageArray[3] == QueriedUser)
                message += ": " + e.Data.RawMessageArray[3];

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, message) });
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
                Title = QueriedUser;
                notifyIcon.Text = QueriedUser;
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InputBox.Background = ZChat.EntryBack;
            InputBox.Foreground = ZChat.EntryFore;
            Document.Background = ZChat.ChatBack;
        }

        private void SendMessage(string input)
        {
            ZChat.IRC.SendMessage(SendType.Message, QueriedUser, input);

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.BracketFore, "<"),
                                                 new ColorTextPair(ZChat.OwnNickFore, ZChat.IRC.Nickname),
                                                 new ColorTextPair(ZChat.BracketFore, ">") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.TextFore, input) });
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
        }

        public void TakeOutgoingMessage(string message)
        {
            SendMessage(message);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ZChat.PropertyChanged -= ZChat_PropertyChanged;
            ZChat.IRC.OnQueryAction -= IRC_OnQueryAction;
            ZChat.IRC.OnQueryMessage -= IRC_OnQueryMessage;
            ZChat.IRC.OnQueryNotice -= IRC_OnQueryNotice;
            ZChat.IRC.OnNickChange -= IRC_OnNickChange;
            ZChat.IRC.OnQuit -= IRC_OnQuit;
        }
    }
}
