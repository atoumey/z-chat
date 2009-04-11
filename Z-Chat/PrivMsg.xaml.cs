using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Meebey.SmartIrc4net;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Net;
using System.Xml.Linq;

namespace ZChat
{
    public partial class PrivMsg : ActivityWindow
    {
        ChannelWindow ChatWindow;
        IrcClient irc;
        string queriedUser;

        public int NextHistoricalEntry;
        public List<string> EntryHistory = new List<string>();

        public PrivMsg(ChannelWindow chatWindow, IrcClient ircClient, string queriedUserName)
        {
            InitializeComponent();
            ChatWindow = chatWindow;
            irc = ircClient;
            queriedUser = queriedUserName;

            RestoreType = chatWindow.RestoreType;

            UpdateTitle();
            EntryHistory.Add("");

            inputTextBox.Background = chatWindow.EntryBack;
            inputTextBox.Foreground = chatWindow.EntryFore;
            chatFlowDoc.Background = chatWindow.ChatBack;
        }

        private void UpdateTitle()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
            {
                Title = "Private message with " + queriedUser;
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            inputTextBox.FontFamily = ChatWindow.Font;
            chatFlowDoc.FontFamily = ChatWindow.Font;

            inputTextBox.Focus();
        }

        private void inputTextBox_KeyUp(object sender, KeyEventArgs e)
        {
           if (e.Key == Key.Enter)
            {
                ParseUserInput();
                inputTextBox.Clear();
            }
            else if (e.Key == Key.Up)
            {
                inputTextBox.Text = EntryHistory[NextHistoricalEntry];
                inputTextBox.CaretIndex = inputTextBox.Text.Length;
                NextHistoricalEntry++;
                if (NextHistoricalEntry == EntryHistory.Count)
                    NextHistoricalEntry = 0;
            }
            else if (e.Key == Key.Down)
            {
                NextHistoricalEntry--;
                if (NextHistoricalEntry == -1)
                    NextHistoricalEntry = EntryHistory.Count - 1;
                if (NextHistoricalEntry == 0)
                    inputTextBox.Text = EntryHistory[EntryHistory.Count - 1];
                else
                    inputTextBox.Text = EntryHistory[NextHistoricalEntry - 1];
            }
        }

        private void ParseUserInput()
        {
            try
            {
                string input = inputTextBox.Text;
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
                    irc.SendMessage(SendType.Action, queriedUser, action);

                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "* "),
                                                     new ColorTextPair(ChatWindow.TextFore, irc.Nickname) },
                               new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, action) });
                    }));
                }
                else if (words[0].Equals("/raw", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 6)
                        irc.WriteLine(input.Substring(5));
                    else
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "! Error:") },
                                   new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "command syntax is '/raw <raw IRC message>'.") });
                        }));
                }
                else if (words[0].Equals("/np", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (string.IsNullOrEmpty(ChatWindow.LastFMUserName))
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "!") },
                                   new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "You must choose a Last.fm username on the options dialog.") });
                        }));
                    else
                    {
                        WebClient client = new WebClient();
                        client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LastFMdownloadComplete);
                        client.DownloadStringAsync(new Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + ChatWindow.LastFMUserName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"));
                    }
                }
                else if (input.StartsWith("/"))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "! Error:") },
                               new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "command not recognized.") });
                    }));
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    irc.SendMessage(SendType.Message, queriedUser, inputTextBox.Text);

                    Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                    {
                        Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.BracketFore, "<"),
                                                     new ColorTextPair(ChatWindow.OwnNickFore, irc.Nickname),
                                                     new ColorTextPair(ChatWindow.BracketFore, ">") },
                               new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, inputTextBox.Text) });
                    }));
                }

                if (!string.IsNullOrEmpty(input))
                {
                    NextHistoricalEntry = 1;
                    if (EntryHistory.Count == 100)
                    {
                        EntryHistory.RemoveAt(EntryHistory.Count - 1);
                    }
                    EntryHistory.Insert(1, input);
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
                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "!") },
                                   new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "You haven't submitted a song to Last.fm in the last 30 minutes.  Maybe Last.fm submission service is down?") });
                        }));
                    }
                    else
                    {
                        string action = "is listening to " + track.name + " by " + track.artist;
                        irc.SendMessage(SendType.Action, queriedUser, action);

                        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                        {
                            Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "* "),
                                                     new ColorTextPair(ChatWindow.TextFore, irc.Nickname) },
                                   new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, action) });
                        }));
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new VoidDelegate(delegate
                {
                    Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "!") },
                           new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, ex.Message) });
                }));
            }
        }

        public void Output(ColorTextPair[] sourcePairs, ColorTextPair[] textPairs)
        {
            string timeStamp;
            timeStamp = DateTime.Now.ToString(ChatWindow.TimeStampFormat);
            TimeSourceTextGroup group = new TimeSourceTextGroup(timeStamp, sourcePairs, textPairs);

            AddOutput(group);

            DependencyObject DO = VisualTreeHelper.GetChild(chatScrollViewer, 0);
            while (!(DO is ScrollViewer))
                DO = VisualTreeHelper.GetChild(DO, 0);
            ScrollViewer sv = DO as ScrollViewer;

            if (sv.VerticalOffset == sv.ScrollableHeight)
                sv.ScrollToBottom();
        }

        protected Thickness paragraphPadding = new Thickness(2.0, 0.0, 0.0, 0.0);
        protected static SolidColorBrush LinkBrush = Brushes.LightBlue;
        protected static SolidColorBrush TextBrush = Brushes.Black;
        protected static Regex HyperlinkPattern = new Regex("(^|[ ]|((https?|ftp):\\/\\/))(([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+)|localhost|([a-zA-Z0-9\\-]+\\.)*[a-zA-Z0-9\\-]+\\.(com|net|org|info|biz|gov|name|edu|[a-zA-Z][a-zA-Z]))(:[0-9]+)?((\\/|\\?)[^ \"]*[^ ,;\\.:\">)])?", RegexOptions.Compiled);
        public void AddOutput(TimeSourceTextGroup group)
        {
            Paragraph p = new Paragraph();
            p.Padding = paragraphPadding;
            p.TextAlignment = TextAlignment.Left;

            Span timeSourceSpan = new Span();
            Run timeRun = new Run(group.Time);
            timeRun.Foreground = ChatWindow.TimeFore;
            p.Inlines.Add(timeRun);

            ColorTextPair[] allPairs = new ColorTextPair[group.Source.Length + 1 + group.Text.Length];
            for (int ii = 0; ii < group.Source.Length; ii++) allPairs[ii] = group.Source[ii];
            allPairs[group.Source.Length] = new ColorTextPair(TextBrush, " ");
            for (int ii = 0; ii < group.Text.Length; ii++) allPairs[ii + group.Source.Length + 1] = group.Text[ii];

            ChatWindow.AddInlines(p.Inlines, allPairs, true);

            double indent = new FormattedText(timeRun.Text + ChatWindow.PairsToPlainText(group.Source) + "W",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(chatFlowDoc.FontFamily, chatFlowDoc.FontStyle, chatFlowDoc.FontWeight, chatFlowDoc.FontStretch),
                12.0, Brushes.Black).Width;
            p.Margin = new Thickness(indent, 0.0, 0.0, 0.0);
            p.TextIndent = indent * -1;

            chatFlowDoc.Blocks.Add(p);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ChatWindow.PrivWindowDied(queriedUser);
        }

        internal void NickChange(string newNick)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new VoidDelegate(delegate
            {
                Output(new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, "!") },
                       new ColorTextPair[] { new ColorTextPair(ChatWindow.TextFore, queriedUser + " changed their name to " + newNick) });
            }));

            queriedUser = newNick;
            UpdateTitle();

            ShowActivity();
        }

        internal void ChangeClickRestoreType(ClickRestoreType value)
        {
            RestoreType = value;
        }
    }
}
