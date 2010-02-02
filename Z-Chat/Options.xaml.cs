using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Input;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window, INotifyPropertyChanged
    {
        public App ZChat { get { return _zchat; } set { _zchat = value; FirePropertyChanged("ZChat"); } }
        private App _zchat;

        private ScriptScope _pythonScope;
        private MemoryStream _pythonOutput = new MemoryStream();

        public Options(App parent) : base()
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("ZChat.IRC.ico"));
            ZChat = parent;

            EntryHistory.Add("");

            _pythonScope = ZChat.PythonEngine.CreateScope();
            ZChat.PythonEngine.Runtime.IO.SetOutput(_pythonOutput, Encoding.UTF8);
            _pythonScope.SetVariable("zchat", ZChat);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HideAllGrids();
            DataContext = this;
            generalTreeItem.IsSelected = true;

            channelTextBox.Text = ZChat.FirstChannel;
            nickNameTextBox.Text = ZChat.InitialNickname;
            serverTextBox.Text = ZChat.Server;
            serverPortTextBox.Text = ZChat.ServerPort.ToString();
            channelKeyTextBox.Text = ZChat.FirstChannelKey;

            saveConnectionInfoCheckBox.IsChecked = ZChat.SaveConnectionInfo;

            if (ZChat.RestoreType == ClickRestoreType.SingleClick)
            {
                singleClickRestore.IsChecked = true;
                doubleClickRestore.IsChecked = false;
            }
            else
            {
                singleClickRestore.IsChecked = false;
                doubleClickRestore.IsChecked = true;
            }

            joinsQuitsHighlight.IsChecked = ZChat.HighlightTrayIconForJoinsAndQuits;

            UsersBack.Background = ZChat.UsersBack;
            UsersFore.Background = ZChat.UsersFore;
            EntryBack.Background = ZChat.EntryBack;
            EntryFore.Background = ZChat.EntryFore;
            ChatBack.Background = ZChat.ChatBack;
            TimeFore.Background = ZChat.TimeFore;
            NickFore.Background = ZChat.NickFore;
            BracketFore.Background = ZChat.BracketFore;
            TextFore.Background = ZChat.TextFore;
            QueryTextFore.Background = ZChat.QueryTextFore;
            OwnNickFore.Background = ZChat.OwnNickFore;
            LinkFore.Background = ZChat.LinkFore;

            fontsCombo.ItemsSource = Fonts.SystemFontFamilies;
            fontsCombo.SelectedValue = ZChat.Font.Source;

            timeFormatBox.Text = ZChat.TimeStampFormat;
            windowsForPrivMsgs.IsChecked = ZChat.WindowsForPrivMsgs;
            lastfmUserBox.Text = ZChat.LastFMUserName;
            hyperlinkPatternBox.Text = ZChat.HyperlinkPattern;

            //foreach (Plugin plugin in ZChat.LoadedScripts)
            //    foreach (Grid pluginGrid in plugin.GetOptionGrids())
            //        mainGrid.Children.Add(pluginGrid);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            _pythonOutput.Close();
            Close();
        }

        private void SaveOptions()
        {
            ZChat.FirstChannel = channelTextBox.Text;
            ZChat.InitialNickname = nickNameTextBox.Text;
            ZChat.Server = serverTextBox.Text;
            try { ZChat.ServerPort = int.Parse(serverPortTextBox.Text); }
            catch { serverPortTextBox.Text = ZChat.ServerPort.ToString(); }
            ZChat.FirstChannelKey = channelKeyTextBox.Text;

            ZChat.SaveConnectionInfo = saveConnectionInfoCheckBox.IsChecked.Value;

            if (singleClickRestore.IsChecked.Value)
                ZChat.RestoreType = ClickRestoreType.SingleClick;
            else
                ZChat.RestoreType = ClickRestoreType.DoubleClick;

            ZChat.HighlightTrayIconForJoinsAndQuits = joinsQuitsHighlight.IsChecked.Value;

            ZChat.UsersBack = (SolidColorBrush)UsersBack.Background;
            ZChat.UsersFore = (SolidColorBrush)UsersFore.Background;
            ZChat.EntryBack = (SolidColorBrush)EntryBack.Background;
            ZChat.EntryFore = (SolidColorBrush)EntryFore.Background;
            ZChat.ChatBack = (SolidColorBrush)ChatBack.Background;
            ZChat.TimeFore = (SolidColorBrush)TimeFore.Background;
            ZChat.NickFore = (SolidColorBrush)NickFore.Background;
            ZChat.BracketFore = (SolidColorBrush)BracketFore.Background;
            ZChat.TextFore = (SolidColorBrush)TextFore.Background;
            ZChat.QueryTextFore = (SolidColorBrush)QueryTextFore.Background;
            ZChat.OwnNickFore = (SolidColorBrush)OwnNickFore.Background;
            ZChat.LinkFore = (SolidColorBrush)LinkFore.Background;

            ZChat.Font = (FontFamily)fontsCombo.SelectedItem;

            ZChat.TimeStampFormat = timeFormatBox.Text;
            ZChat.WindowsForPrivMsgs = windowsForPrivMsgs.IsChecked.Value;
            ZChat.LastFMUserName = lastfmUserBox.Text;
            ZChat.HyperlinkPattern = hyperlinkPatternBox.Text;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            DialogResult = true;
            _pythonOutput.Close();
            Close();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            System.Windows.Forms.ColorDialog colorPicker = new System.Windows.Forms.ColorDialog();
            colorPicker.Color = System.Drawing.Color.FromArgb(((SolidColorBrush)b.Background).Color.R, ((SolidColorBrush)b.Background).Color.G, ((SolidColorBrush)b.Background).Color.B);
            colorPicker.FullOpen = true;
            colorPicker.ShowDialog();

            b.Background = new SolidColorBrush(Color.FromRgb(colorPicker.Color.R, colorPicker.Color.G, colorPicker.Color.B));
        }

        private void TimeFormatHelp_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(delegate
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("http://msdn.microsoft.com/en-us/library/8kb3ddd4(VS.71).aspx");
                    psi.UseShellExecute = true;
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            })).Start();
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OK_Click(this, e);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                Cancel_Click(this, e);
            }
        }

        private void HideAllGrids()
        {
            appearanceGrid.Visibility = Visibility.Hidden;
            highlightingGrid.Visibility = Visibility.Hidden;
            windowsGrid.Visibility = Visibility.Hidden;
            miscGrid.Visibility = Visibility.Hidden;
            generalGrid.Visibility = Visibility.Hidden;
            colorsGrid.Visibility = Visibility.Hidden;
            systemTrayGrid.Visibility = Visibility.Hidden;
            scriptGrid.Visibility = Visibility.Hidden;
            pythonConsoleGrid.Visibility = Visibility.Hidden;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HideAllGrids();

            if (e.NewValue == appearanceTreeItem)
                appearanceGrid.Visibility = Visibility.Visible;
            if (e.NewValue == highlightingTreeItem)
                highlightingGrid.Visibility = Visibility.Visible;
            if (e.NewValue == windowsTreeItem)
                windowsGrid.Visibility = Visibility.Visible;
            if (e.NewValue == miscTreeItem)
                miscGrid.Visibility = Visibility.Visible;
            if (e.NewValue == generalTreeItem)
                generalGrid.Visibility = Visibility.Visible;
            if (e.NewValue == colorsTreeItem)
                colorsGrid.Visibility = Visibility.Visible;
            if (e.NewValue == systemTrayTreeItem)
                systemTrayGrid.Visibility = Visibility.Visible;
            if (e.NewValue == scriptTreeItem)
                scriptGrid.Visibility = Visibility.Visible;
            if (e.NewValue == pythonConsoleItem)
                pythonConsoleGrid.Visibility = Visibility.Visible;
        }

        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = ofd.FileName;
                ZChat.CreatePlugin(path);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public int NextHistoricalEntry;
        public List<string> EntryHistory = new List<string>();

        StringBuilder input = new StringBuilder(255);
        private void consoleInput_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (!string.IsNullOrEmpty(consoleInput.Text))
                {
                    NextHistoricalEntry = 1;
                    if (EntryHistory.Count == 100)
                    {
                        EntryHistory.RemoveAt(EntryHistory.Count - 1);
                    }
                    EntryHistory.Insert(1, consoleInput.Text);
                }

                e.Handled = true;
                //consoleOutput.AppendText(">> " + consoleInput.Text + Environment.NewLine);

                input.Append(consoleInput.Text);
                if (run(input.ToString()))
                {
                    input.AppendLine();
                    consolePromptLabel.Text = "...";
                }
                else
                {
                    input.Length = 0;
                    consolePromptLabel.Text = ">>>";
                }

                //try
                //{
                //    ScriptSource source = ZChat.PythonEngine.CreateScriptSourceFromString(consoleInput.Text,
                //        SourceCodeKind.InteractiveCode);
                    
                //    object o = source.Execute(_pythonScope);

                //    int length = (int)_pythonOutput.Length;
                //    if (length > 0)
                //    {
                //        Byte[] bytes = new Byte[length];

                //        _pythonOutput.Seek(0, SeekOrigin.Begin);
                //        _pythonOutput.Read(bytes, 0, length);

                //        consoleOutput.AppendText(Encoding.UTF8.GetString(bytes, 0, length));
                //        _pythonOutput.SetLength(0);
                //    }
                //    else
                //        consoleOutput.AppendText(o.ToString() + Environment.NewLine);
                //}
                //catch (Exception ex)
                //{
                //    consoleOutput.AppendText(ex.Message + Environment.NewLine);
                //}

                consoleOutput.ScrollToEnd();
                consoleInput.Clear();
            }
            else if (e.Key == Key.Up)
            {
                consoleInput.Text = EntryHistory[NextHistoricalEntry];
                consoleInput.CaretIndex = consoleInput.Text.Length;
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
                    consoleInput.Text = EntryHistory[EntryHistory.Count - 1];
                else
                    consoleInput.Text = EntryHistory[NextHistoricalEntry - 1];
            }
        }

        void writePythonOutputToScreen()
        {
            int length = (int)_pythonOutput.Length;
            if (length > 0)
            {
                byte[] bytes = new byte[length];

                _pythonOutput.Seek(0, SeekOrigin.Begin);
                _pythonOutput.Read(bytes, 0, length);

                consoleOutput.AppendText(Encoding.UTF8.GetString(bytes, 0, length));
                _pythonOutput.SetLength(0);
            }
        }

        bool allow_incomplete(string[] lines)
        {
            if (string.IsNullOrEmpty(lines[lines.Length -1]))
            {
                lines[lines.Length-1] = "";
                return true;
            }
            return false;
        }
        
        bool is_complete(string code, bool allow_incomplete)
        {
            ScriptSource cmd = ZChat.PythonEngine.CreateScriptSourceFromString(code + Environment.NewLine, SourceCodeKind.InteractiveCode);
            ScriptCodeParseResult props = cmd.GetCodeProperties(ZChat.PythonEngine.GetCompilerOptions());
            if (SourceCodePropertiesUtils.IsCompleteOrInvalid(props, allow_incomplete))
                return props != ScriptCodeParseResult.Empty;
            else
                return false;
        }
            
        void write_input(string[] lines)
        {
            if (lines.Length == 1)
                consoleOutput.AppendText(">>>" + lines[0] + Environment.NewLine);
            else
            {
                consoleOutput.AppendText("..." + lines[lines.Length - 1] + Environment.NewLine);
            }
        }

        bool run(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                consoleOutput.AppendText(">>>" + Environment.NewLine);
                return false;
            }

            string[] lines = code.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            if (is_complete(code, allow_incomplete(lines)))
            {
                write_input(lines);
                _run(code, lines.Length > 1);
                return false;
            }
            else
            {
                write_input(lines);
                return true;
            }
        }
   
        void _run(string code, bool multiline)
        {
            try
            {
                if (multiline)
                    run_multiline(code);
                else
                    run_singleline(code);
            }
            catch (Exception e)
            {
                handle_exception(e);
            }
        }
   
        void run_singleline(string code)
        {
            object returnVal = null;

            try
            {
                ScriptSource src = ZChat.PythonEngine.CreateScriptSourceFromString(code, SourceCodeKind.InteractiveCode);
                returnVal = src.Execute(_pythonScope);
                writePythonOutputToScreen();
                //ret = eval(code, self.namespace);
            }
            catch (SyntaxErrorException syn)
            {
                run_multiline(code);
            }
            finally
            {
                if (returnVal != null)
                {
                    consoleOutput.AppendText(returnVal.ToString() + Environment.NewLine);
                    if (Dispatcher.CheckAccess())
                    {
                        _pythonScope.SetVariable("_", returnVal);
                        //self.namespace['_'] = ret;
                    }
                }
            }
        }

        void run_multiline(string code)
        {
            ScriptSource src = ZChat.PythonEngine.CreateScriptSourceFromString(code, SourceCodeKind.InteractiveCode);
            src.Execute(_pythonScope);
            writePythonOutputToScreen();
            //exec code in self.namespace;
        }
            
        void handle_exception(Exception e)
        {
            print_exception(e);
        }
            
        void print_exception(Exception clsException)
        {
            ExceptionOperations exc_service = ZChat.PythonEngine.GetService<ExceptionOperations>();
     
            string traceback = exc_service.FormatException(clsException);        
           
            List<string> linesToLookAt = new List<string>();
            foreach (string line in traceback.Split(new string[]{Environment.NewLine}, StringSplitOptions.None))
            {
                if (!line.Contains("in <string>") && !line.Contains("zchat\\"))
                    linesToLookAt.Add(line);
            }
            if (linesToLookAt.Count == 2)
                linesToLookAt.RemoveAt(0);
            //lines = [line for line in traceback.splitlines() if 'in <string>' not in line and 'silvershell\\' not in line]
            //if len(lines) == 2:
            //    lines = lines[1:]       
            
            consoleOutput.AppendText(string.Join(Environment.NewLine, linesToLookAt.ToArray()));
            //if (Preferences.ExceptionDetail)
            //{
            //    consoleOutput.AppendText(Environment.NewLine + "CLR Exception: ");
            //    consoleOutput.AppendText(clsException.ToString());
            //}
            
            consoleOutput.AppendText(Environment.NewLine);
        }
    }
}
