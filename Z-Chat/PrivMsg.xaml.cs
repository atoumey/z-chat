using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Meebey.SmartIrc4net;

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
        public PrivMsg(Chat zchat, string queriedUserName, IrcEventArgs firstMessage)
            : this(zchat, queriedUserName)
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
        public PrivMsg(Chat zchat, string queriedUserName, string firstMessage)
            : this(zchat, queriedUserName)
        {
            SendMessage(firstMessage);
        }

        protected PrivMsg(Chat zchat, string queriedUserName) : base(zchat)
        {
            WindowIconName_NoActivity = "ZChat.IRC.ico";
            WindowIconName_Activity = "ZChat.IRCgreen.ico";
            TrayIconName_NoActivity = "ZChat.IRC.ico";
            TrayIconName_Activity = "ZChat.IRCgreen.ico";

            ZChat.Options.PropertyChanged += ZChat_PropertyChanged;
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

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, e.Who + " is now away (" + e.AwayMessage + ")") });
        }

        void IRC_OnErrorMessage(object sender, IrcEventArgs e)
        {
            string message = e.Data.Message;
            if (e.Data.ReplyCode == ReplyCode.ErrorNicknameInUse && e.Data.RawMessageArray[3] == QueriedUser)
                message += ": " + e.Data.RawMessageArray[3];

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, message) });
        }

        void IRC_OnQuit(object sender, QuitEventArgs e)
        {
            if (e.Who != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, "!") },
                           new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, e.Who + " quit (" + e.QuitMessage + ")") });

            if (ZChat.Options.HighlightTrayIconForJoinsAndQuits)
                ShowActivity();
        }

        void IRC_OnQueryNotice(object sender, IrcEventArgs e)
        {
            if (e.Data.Nick != QueriedUser) return;

            ColorTextPair[] source;
            if (e.Data.Nick == null)
                source = new ColorTextPair[] { };
            else
                source = new ColorTextPair[] { new ColorTextPair(ZChat.Options.QueryTextFore, "*"),
                                         new ColorTextPair(ZChat.Options.QueryTextFore, e.Data.Nick),
                                         new ColorTextPair(ZChat.Options.QueryTextFore, "*") };

            Output(source, new ColorTextPair[] { new ColorTextPair(ZChat.Options.QueryTextFore, e.Data.Message) });
            ShowActivity();
        }

        void IRC_OnQueryMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Nick != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.BracketFore, "<"),
                                                         new ColorTextPair(ZChat.Options.NickFore, e.Data.Nick),
                                                         new ColorTextPair(ZChat.Options.BracketFore, ">") },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, e.Data.Message) });
            ShowActivity();
        }

        void IRC_OnQueryAction(object sender, ActionEventArgs e)
        {
            if (e.Data.Nick != QueriedUser) return;

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.BracketFore, "* "),
                                                         new ColorTextPair(ZChat.Options.TextFore, e.Data.Nick) },
                                   new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, e.ActionMessage.Substring(1)) });
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
            InputBox.Background = ZChat.Options.EntryBack;
            InputBox.Foreground = ZChat.Options.EntryFore;
            Document.Background = ZChat.Options.ChatBack;
        }

        private void SendMessage(string input)
        {
            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.BracketFore, "<"),
                                                 new ColorTextPair(ZChat.Options.OwnNickFore, ZChat.IRC.Nickname),
                                                 new ColorTextPair(ZChat.Options.BracketFore, ">") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, input) });
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

            Output(new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, "!") },
                   new ColorTextPair[] { new ColorTextPair(ZChat.Options.TextFore, e.OldNickname + " changed their name to " + e.NewNickname) });

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
            ZChat.Options.PropertyChanged -= ZChat_PropertyChanged;
            ZChat.IRC.OnQueryAction -= IRC_OnQueryAction;
            ZChat.IRC.OnQueryMessage -= IRC_OnQueryMessage;
            ZChat.IRC.OnQueryNotice -= IRC_OnQueryNotice;
            ZChat.IRC.OnNickChange -= IRC_OnNickChange;
            ZChat.IRC.OnQuit -= IRC_OnQuit;
        }
    }
}
