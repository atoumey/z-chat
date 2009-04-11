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
    public partial class PrivMsg : ChatWindow
    {
        ChannelWindow ChatWindow;
        IrcClient irc;
        string queriedUser;

        public PrivMsg(ChannelWindow chatWindow, IrcClient ircClient, string queriedUserName)
        {
            InitializeComponent();
            ChatWindow = chatWindow;
            irc = ircClient;
            queriedUser = queriedUserName;

            RestoreType = chatWindow.RestoreType;

            UpdateTitle();

            InputBox.Background = chatWindow.EntryBack;
            InputBox.Foreground = chatWindow.EntryFore;
            Document.Background = chatWindow.ChatBack;
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
            InputBox.FontFamily = ChatWindow.Font;
            Document.FontFamily = ChatWindow.Font;
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

        private void Window_Initialized(object sender, EventArgs e)
        {
            Document = chatFlowDoc;
            DocumentScrollViewer = chatScrollViewer;
            InputBox = inputTextBox;

            UserInput += ParseUserInput;
        }
    }
}
