using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace ZChat
{
    /// <summary>
    /// A Window with chat output displayed in a FlowDocument, input shown and accepted through a TextBox,
    /// and a list of chatters whose names can be tab-completed.
    /// The input box also allows scrolling of input history with the up+down keys.
    /// </summary>
    public class ChatWindow : ActivityWindow
    {
        public delegate void InputDelegate(ChatWindow sender, string input);
        public event InputDelegate UserInput;

        protected FlowDocument Document;
        protected FlowDocumentScrollViewer DocumentScrollViewer;

        protected Regex HyperlinkRegex;

        public static int OUTPUT_MAX_LINES = 1000;
        protected int OutputCount = 0;

        protected TextBox InputBox
        {
            get { return _inputBox; }
            set
            {
                _inputBox = value;
                _inputBox.KeyDown += InputBox_KeyDown;
                _inputBox.KeyUp += InputBox_KeyUp;
            }
        }
        private TextBox _inputBox;

        protected List<string> Users = new List<string>();

        /// <summary>
        /// Finds the nick in the user list, ignoring status symbols such as @
        /// </summary>
        /// <param name="nick"></param>
        /// <returns></returns>
        protected bool UsersContains(string nick)
        {
            foreach (string user in Users)
            {
                string noSymbol = user;
                if (user.StartsWith("@") || user.StartsWith("+") || user.StartsWith("%"))
                    noSymbol = user.Substring(1);
                if (noSymbol == nick)
                    return true;
            }
            return false;
        }

        public ChatWindow() { }

        public ChatWindow(App app) : base(app)
        {
            ZChat.PropertyChanged += ZChat_PropertyChanged;

            HyperlinkRegex = new Regex(ZChat.HyperlinkPattern, RegexOptions.Compiled);
            EntryHistory.Add("");

            Loaded += ChatWindow_Loaded;
            Activated += ChatWindow_Activated;
        }

        void ZChat_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EntryBack")
                InputBox.Background = ZChat.EntryBack;
            if (e.PropertyName == "EntryFore")
                InputBox.Foreground = ZChat.EntryFore;
            if (e.PropertyName == "ChatBack")
                Document.Background = ZChat.ChatBack;
            if (e.PropertyName == "HyperlinkPattern")
                HyperlinkRegex = new Regex(ZChat.HyperlinkPattern, RegexOptions.Compiled);
            if (e.PropertyName == "Font")
            {
                InputBox.FontFamily = ZChat.Font;
                Document.FontFamily = ZChat.Font;
            }
        }

        void ChatWindow_Activated(object sender, EventArgs e)
        {
            InputBox.Focus();
        }

        private void ChatWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InputBox.Background = ZChat.EntryBack;
            InputBox.Foreground = ZChat.EntryFore;
            Document.Background = ZChat.ChatBack;
            InputBox.FontFamily = ZChat.Font;
            Document.FontFamily = ZChat.Font;

            InputBox.Focus();
        }

        public int NextHistoricalEntry;
        public List<string> EntryHistory = new List<string>();

        private List<string> NickCompletionList;
        private int CurrentNickCompletion;

        private void FindMatchingNicks(string nickPart)
        {
            NickCompletionList = new List<string>();
            foreach (string nick in Users)
            {
                string actualNick;
                if (nick.StartsWith("@") || nick.StartsWith("+") || nick.StartsWith("%"))
                    actualNick = nick.Substring(1);
                else
                    actualNick = nick;
                if (actualNick.StartsWith(nickPart, StringComparison.CurrentCultureIgnoreCase))
                    NickCompletionList.Add(actualNick);
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                int lastSpace = InputBox.Text.LastIndexOf(" ");
                if (lastSpace != -1 && InputBox.Text[lastSpace - 1] == ':' && lastSpace == InputBox.Text.Length - 1)
                {
                    lastSpace = InputBox.Text.Substring(0, lastSpace).LastIndexOf(" ");
                }

                if (NickCompletionList == null)
                {
                    string nickPart;
                    nickPart = InputBox.Text.Substring(lastSpace + 1);
                    if (string.IsNullOrEmpty(nickPart))
                    {
                        e.Handled = true;
                        return;
                    }
                    FindMatchingNicks(nickPart);
                    CurrentNickCompletion = 0;
                }

                if (NickCompletionList.Count > 0)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        CurrentNickCompletion--;
                        if (CurrentNickCompletion == -1)
                            CurrentNickCompletion = NickCompletionList.Count - 1;
                        if (CurrentNickCompletion == 0)
                            InputBox.Text = InputBox.Text.Substring(0, lastSpace + 1) + NickCompletionList[NickCompletionList.Count - 1];
                        else
                            InputBox.Text = InputBox.Text.Substring(0, lastSpace + 1) + NickCompletionList[CurrentNickCompletion - 1];
                    }
                    else
                    {
                        InputBox.Text = InputBox.Text.Substring(0, lastSpace + 1) + NickCompletionList[CurrentNickCompletion];
                        CurrentNickCompletion++;
                        if (CurrentNickCompletion >= NickCompletionList.Count)
                            CurrentNickCompletion = 0;
                    }

                    if (lastSpace == -1)
                        InputBox.Text += ": ";
                }
                InputBox.CaretIndex = InputBox.Text.Length;
                e.Handled = true;
            }
        }

        private void InputBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.LeftShift && e.Key != Key.RightShift)
                NickCompletionList = null;
            if (e.Key == Key.Enter)
            {
                if (UserInput != null)
                    UserInput(this, InputBox.Text);
                
                if (!string.IsNullOrEmpty(InputBox.Text))
                {
                    NextHistoricalEntry = 1;
                    if (EntryHistory.Count == 100)
                    {
                        EntryHistory.RemoveAt(EntryHistory.Count - 1);
                    }
                    EntryHistory.Insert(1, InputBox.Text);
                }
                
                InputBox.Clear();
            }
            else if (e.Key == Key.Up)
            {
                InputBox.Text = EntryHistory[NextHistoricalEntry];
                InputBox.CaretIndex = InputBox.Text.Length;
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
                    InputBox.Text = EntryHistory[EntryHistory.Count - 1];
                else
                    InputBox.Text = EntryHistory[NextHistoricalEntry - 1];
            }
        }

        public void Output(string output)
        {
            Output(new ColorTextPair[] { new ColorTextPair(Brushes.Black, "") }, new ColorTextPair[] { new ColorTextPair(Brushes.Black, output) });
        }

        public void Output(ColorTextPair[] sourcePairs, ColorTextPair[] textPairs)
        {
            if (Document == null || DocumentScrollViewer == null) return;

            OutputCount++;
            if (OutputCount > OUTPUT_MAX_LINES)
                Document.Blocks.Remove(Document.Blocks.FirstBlock);

            string timeStamp;
            timeStamp = DateTime.Now.ToString(ZChat.TimeStampFormat);
            TimeSourceTextGroup group = new TimeSourceTextGroup(timeStamp, sourcePairs, textPairs);

            Dispatcher.BeginInvoke(new VoidDelegate(delegate
            {
                AddOutput(group);

                if (VisualTreeHelper.GetChildrenCount(DocumentScrollViewer) > 0)
                {
                    DependencyObject DO = VisualTreeHelper.GetChild(DocumentScrollViewer, 0);
                    while (!(DO is ScrollViewer))
                        DO = VisualTreeHelper.GetChild(DO, 0);
                    ScrollViewer sv = DO as ScrollViewer;

                    if (sv.VerticalOffset == sv.ScrollableHeight)
                        sv.ScrollToBottom();
                }
            }));
        }

        protected Thickness paragraphPadding = new Thickness(2.0, 0.0, 0.0, 0.0);
        public void AddOutput(TimeSourceTextGroup group)
        {
            Paragraph p = new Paragraph();
            p.Padding = paragraphPadding;
            p.TextAlignment = TextAlignment.Left;

            Span timeSourceSpan = new Span();
            Run timeRun = new Run(group.Time);
            timeRun.Foreground = ZChat.TimeFore;
            p.Inlines.Add(timeRun);

            ColorTextPair[] allPairs = new ColorTextPair[group.Source.Length + 1 + group.Text.Length];
            for (int ii = 0; ii < group.Source.Length; ii++) allPairs[ii] = group.Source[ii];
            allPairs[group.Source.Length] = new ColorTextPair(ZChat.TextFore, " ");
            for (int ii = 0; ii < group.Text.Length; ii++) allPairs[ii + group.Source.Length + 1] = group.Text[ii];

            AddInlines(p.Inlines, allPairs, true);

            double indent = new FormattedText(timeRun.Text + PairsToPlainText(group.Source) + "W",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(Document.FontFamily, Document.FontStyle, Document.FontWeight, Document.FontStretch),
                12.0, Brushes.Black).Width;
            p.Margin = new Thickness(indent, 0.0, 0.0, 0.0);
            p.TextIndent = indent * -1;

            Document.Blocks.Add(p);
        }

        public void AddInlines(InlineCollection inlineCollection, ColorTextPair[] pairs, bool allowHyperlinks)
        {
            Run run;
            foreach (ColorTextPair pair in pairs)
            {
                bool hasHyperlinks = false;
                if (allowHyperlinks)
                {
                    if (!string.IsNullOrEmpty(pair.Text))
                    {
                        MatchCollection matches = new Regex(ZChat.HyperlinkPattern).Matches(pair.Text);
                        if (matches.Count > 0)
                        {
                            hasHyperlinks = true;
                            string linkText;
                            int linkStart = 0, linkLength = 0;
                            int curPos = 0;
                            foreach (Match match in matches)
                            {
                                if (match.Value.StartsWith(" "))
                                {
                                    linkStart = match.Index + 1;
                                    linkLength = match.Length - 1;
                                }
                                else
                                {
                                    linkStart = match.Index;
                                    linkLength = match.Length;
                                }
                                linkText = pair.Text.Substring(linkStart, linkLength);

                                Hyperlink link = new Hyperlink(new Run(linkText));
                                link.Foreground = ZChat.LinkFore;
                                link.SetValue(KeyboardNavigation.IsTabStopProperty, false);
                                //if (link.FontStyle) link.TextDecorations.Add(TextDecorations.Underline);
                                link.Click += new RoutedEventHandler(link_Click);
                                link.Tag = linkText;
                                run = new Run(pair.Text.Substring(curPos, linkStart - curPos));
                                run.Foreground = pair.Color;
                                if (linkStart > 0) inlineCollection.Add(run);
                                curPos = linkStart + linkLength;
                                inlineCollection.Add(link);
                            }
                            if (curPos < pair.Text.Length)
                            {
                                run = new Run(pair.Text.Substring(curPos, pair.Text.Length - curPos));
                                run.Foreground = pair.Color;
                                inlineCollection.Add(run);
                            }
                        }
                    }
                }
                if (hasHyperlinks == false)
                {
                    AddNonHyperlinkText(inlineCollection, pair.Text, pair.Color);
                    //run = new Run(pair.Text);
                    //run.Foreground = pair.Color;
                    //inlineCollection.Add(run);
                }
            }
        }

        private void AddBoldOrRun(InlineCollection inlines, string text, SolidColorBrush brush, bool bold)
        {
            Run r = new Run(text);
            r.Foreground = brush;

            if (bold)
                inlines.Add(new Bold(r));
            else
                inlines.Add(r);
        }

        private void AddNonHyperlinkText(InlineCollection inlines, string text, SolidColorBrush brush)
        {
            int mostRecentBoldCharPos = 0;
            bool boldOn = false;
            for (int curPos = 0; curPos < text.Length; curPos++)
            {
                if (text[curPos] == (char)2)
                {
                    AddBoldOrRun(inlines, text.Substring(mostRecentBoldCharPos, curPos - mostRecentBoldCharPos), brush, boldOn);
                    mostRecentBoldCharPos = curPos + 1;
                    boldOn = !boldOn;
                }
            }

            AddBoldOrRun(inlines, text.Substring(mostRecentBoldCharPos, text.Length - mostRecentBoldCharPos), brush, boldOn);
        }

        public string PairsToPlainText(ColorTextPair[] colorTextPairs)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ColorTextPair ctp in colorTextPairs)
            {
                sb.Append(ctp.Text);
            }
            return sb.ToString();
        }

        void link_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ParameterizedThreadStart(delegate(object link)
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo(link.ToString());
                    psi.UseShellExecute = true;
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            })).Start(((sender as Hyperlink).Tag as string).Trim());
        }

        public void Clear()
        {
            Document.Blocks.Clear();
        }
    }

    public struct TimeSourceTextGroup
    {
        public string Time;
        public ColorTextPair[] Source;
        public ColorTextPair[] Text;

        public TimeSourceTextGroup(string time, ColorTextPair[] source, ColorTextPair[] text)
        {
            Time = time;
            Source = source;
            Text = text;
        }
    }

    public struct ColorTextPair
    {
        public SolidColorBrush Color;
        public string Text;

        public ColorTextPair(SolidColorBrush color, string text)
        {
            Color = color;
            Text = text;
        }

        public ColorTextPair[] Append(SolidColorBrush color, string text)
        {
            return new ColorTextPair[] { this, new ColorTextPair(color, text) };
        }
    }
}
