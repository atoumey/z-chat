using System;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.IO;
using Meebey.SmartIrc4net;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;
using System.Net;
using System.Xml.Linq;
using System.Linq;

using IronPython;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using IronPython.Runtime;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Input;

namespace ZChat
{
    delegate void VoidDelegate();

    public class Chat : INotifyPropertyChanged
    {
        //If no config file is found (or the file does not contain connection info),
        // a connection dialog will be presented.
        //These will be the defaults for the connection dialog.
        public const string CONFIG_FILE_NAME = "zchat_config.txt";
        public const string FIRST_CHANNEL = "#test";
        public const string FIRST_CHANNEL_KEY = null;
        public const string SERVER_ADDRESS = "irc.mibbit.com";
        public const int SERVER_PORT = 6667;

        // The MainOutputWindow is where we output messages that are not specific to a particular
        // channel or query.
        public ChannelWindow MainOutputWindow { get; set; }
        public IrcClient IRC = new IrcClient();

        public ObservableCollection<PrivMsg> queryWindows = new ObservableCollection<PrivMsg>();
        public ObservableCollection<ChannelWindow> channelWindows = new ObservableCollection<ChannelWindow>();

        #region Options
        public string FirstChannel;
        public string FirstChannelKey;
        public string InitialNickname;
        public string Server;
        public int ServerPort;
        public bool SaveConnectionInfo = true;
        public SolidColorBrush EntryBack { get { return _entryBack; } set { _entryBack = value; FirePropertyChanged("EntryBack"); } }
        private SolidColorBrush _entryBack = Brushes.White;
        public SolidColorBrush EntryFore { get { return _entryFore; } set { _entryFore = value; FirePropertyChanged("EntryFore"); } }
        private SolidColorBrush _entryFore = Brushes.Black;
        public SolidColorBrush ChatBack { get { return _chatBack; } set { _chatBack = value; FirePropertyChanged("ChatBack"); } }
        private SolidColorBrush _chatBack = Brushes.White;
        public SolidColorBrush TimeFore = Brushes.Black;
        public SolidColorBrush NickFore = Brushes.Black;
        public SolidColorBrush BracketFore = Brushes.Black;
        public SolidColorBrush TextFore { get { return _textFore; } set { _textFore = value; FirePropertyChanged("TextFore"); } }
        public SolidColorBrush _textFore = Brushes.Black;
        public SolidColorBrush QueryTextFore = Brushes.Maroon;
        public SolidColorBrush OwnNickFore = Brushes.Green;
        public SolidColorBrush LinkFore { get { return _linkFore; } set { _linkFore = value; FirePropertyChanged("LinkFore"); } }
        public SolidColorBrush _linkFore = Brushes.Black;
        public SolidColorBrush UsersBack { get { return _usersBack; } set { _usersBack = value; FirePropertyChanged("UsersBack"); } }
        private SolidColorBrush _usersBack = Brushes.White;
        public SolidColorBrush UsersFore { get { return _usersFore; } set { _usersFore = value; FirePropertyChanged("UsersFore"); } }
        private SolidColorBrush _usersFore = Brushes.Black;
        public string TimeStampFormat = "HH:mm:ss ";
        public ClickRestoreType RestoreType { get { return _restoreType; } set { _restoreType = value; FirePropertyChanged("RestoreType"); } }
        private ClickRestoreType _restoreType = ClickRestoreType.SingleClick;
        public bool HighlightTrayIconForJoinsAndQuits = true;
        public FontFamily Font { get { return _font; } set { _font = value; FirePropertyChanged("Font"); } }
        private FontFamily _font = new FontFamily("Courier New");
        public bool WindowsForPrivMsgs = false;
        public string LastFMUserName = "";
        public string HyperlinkPattern { get { return _hyperlinkPattern; } set { _hyperlinkPattern = value; FirePropertyChanged("HyperlinkPattern"); } }
        protected string _hyperlinkPattern = "(^|[ ]|((https?|ftp):\\/\\/))(([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+)|localhost|([a-zA-Z0-9\\-]+\\.)*[a-zA-Z0-9\\-]+\\.(com|net|org|info|biz|gov|name|edu|[a-zA-Z][a-zA-Z]))(:[0-9]+)?((\\/|\\?)[^ \"]*[^ ,;\\.:\">)])?\\)?";
        #endregion

        List<string> rawMessages = new List<string>();

        PythonConsole pythonConsole;

        public Chat()
        {
            System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
            item.InputGestureText = "Ctrl+C";
            item.Command = ApplicationCommands.SelectAll;
            item.Header = "Copy";

            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(UnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);
   
            MainOutputWindow = new ChannelWindow(this);
            MainOutputWindow.UserInput += ParseUserInput;
            MainOutputWindow.Closed += new EventHandler(channelWindow_Closed);
            MainOutputWindow.IsMainWindow = true;
            MainOutputWindow.Show();

            LoadConfigurationFile();

            bool proceed = true;
            if (FirstChannel == null || InitialNickname == null || Server == null)
            {
                if (FirstChannel == null) FirstChannel = FIRST_CHANNEL;
                if (FirstChannelKey == null) FirstChannelKey = FIRST_CHANNEL_KEY;
                if (InitialNickname == null) InitialNickname = Environment.UserName;
                if (Server == null) Server = SERVER_ADDRESS;
                if (ServerPort == 0) ServerPort = SERVER_PORT;
                
                proceed = ShowConnectionWindow(MainOutputWindow);
            }

            if (proceed)
            {
                SaveConfigurationFile();

                IRC.ActiveChannelSyncing = true;
                IRC.AutoNickHandling = true;
                IRC.SupportNonRfc = true;
                IRC.Encoding = System.Text.Encoding.UTF8;
                IRC.OnConnected += new EventHandler(IRC_OnConnected);
                IRC.OnQueryAction += new ActionEventHandler(IRC_OnQueryAction);
                IRC.OnQueryMessage += new IrcEventHandler(IRC_OnQueryMessage);
                IRC.OnQueryNotice += new IrcEventHandler(IRC_OnQueryNotice);
                IRC.OnNickChange += new NickChangeEventHandler(IRC_OnNickChange);
                IRC.OnJoin += new JoinEventHandler(IRC_OnJoin);
#if DEBUG
                IRC.OnRawMessage += new IrcEventHandler(IRC_OnRawMessage);
                IRC.OnWriteLine += new WriteLineEventHandler(IRC_OnWriteLine);
#endif
                MainOutputWindow.Channel = FirstChannel;
                MainOutputWindow.ChannelKey = FirstChannelKey;

                channelWindows.Add(MainOutputWindow);

                LoadScripts();

                IRC.Connect(Server, ServerPort);
            }
        }

        public ObservableCollection<ScriptInfo> LoadedScripts { get { return _loadedScripts; } set { _loadedScripts = value; FirePropertyChanged("LoadedScripts"); } }
        private ObservableCollection<ScriptInfo> _loadedScripts = new ObservableCollection<ScriptInfo>();

        public ScriptEngine PythonEngine;

        private void LoadScripts()
        {
            Dictionary<String, Object> options = new Dictionary<string, object>();
            options["DivisionOptions"] = PythonDivisionOptions.New;
            PythonEngine = Python.CreateEngine(options);

            ScriptRuntime runtime = PythonEngine.Runtime;
            runtime.LoadAssembly(GetType().Assembly);
            runtime.LoadAssembly(typeof(String).Assembly);
            runtime.LoadAssembly(typeof(Uri).Assembly);
            runtime.LoadAssembly(typeof(Brushes).Assembly);

            pythonConsole = new PythonConsole(this);
            PythonEngine.Runtime.IO.SetOutput(new MemoryStream(), new TextBoxWriter(pythonConsole.PythonOutput));

            string pluginsDir = Path.Combine(Environment.CurrentDirectory, "scripts");
            if (Directory.Exists(pluginsDir))
                foreach (string path in Directory.GetFiles(pluginsDir))
                {
                    if (path.ToLower().EndsWith(".py"))
                    {
                        CreatePlugin(path);
                    }
                }
        }

        public class ScriptInfo
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Author { get; set; }
            public string Description { get; set; }

            public ScriptScope Scope { get; set; }
        }

        public void CreatePlugin(string path)
        {
            try
            {
                ScriptSource script = PythonEngine.CreateScriptSourceFromFile(path);
                CompiledCode code = script.Compile();
                ScriptInfo info = new ScriptInfo();
                info.Scope = PythonEngine.CreateScope();
                info.Scope.SetVariable("zchat", this);
                code.Execute(info.Scope);

                object somefunc;
                if (!info.Scope.TryGetVariable("info", out somefunc))
                    throw new Exception("No info() function found.  This function must be defined and return a tuple of 4 strings (script name, version, author, description).");
                object val = PythonEngine.Operations.Invoke(somefunc);
                PythonTuple tuple = val as PythonTuple;
                if (tuple == null) throw new Exception("The info() function returned a " + val.GetType().Name + " instead of a tuple of strings.");
                if (tuple.Count != 4) throw new Exception("The info() function returned a " + tuple.Count + "-item tuple instead of a 4-item tuple.  It should return Name, Version, Author, Description.");

                info.Name = tuple[0] as string;
                if (info.Name == null) throw new Exception("The info() function did not return correct data.  The first item in the returned tuple was not a string.  It should be the script name.");
                info.Version = tuple[1] as string;
                if (info.Version == null) throw new Exception("The info() function did not return correct data.  The second item in the returned tuple was not a string.  It should be the script version.");
                info.Author = tuple[2] as string;
                if (info.Author == null) throw new Exception("The info() function did not return correct data.  The third item in the returned tuple was not a string.  It should be the script author.");
                info.Description = tuple[3] as string;
                if (info.Description == null) throw new Exception("The info() function did not return correct data.  The fourth item in the returned tuple was not a string.  It should be the script description.");

                LoadedScripts.Add(info);
            }
            catch (SyntaxErrorException e)
            {
                ExceptionOperations eo = PythonEngine.GetService<ExceptionOperations>();
                string error = eo.FormatException(e);

                string caption = String.Format("Syntax error in \"{0}\"", Path.GetFileName(path));
                MessageBox.Show(error, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error loading the " + Path.GetFileName(path) + " script." + Environment.NewLine + Environment.NewLine + e.Message, 
                    "Error loading script", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void IRC_OnWriteLine(object sender, WriteLineEventArgs e)
        {
            rawMessages.Add(DateTime.Now.ToString("HH:mm:ss.ffff ") + e.Line);
        }
        
        void IRC_OnJoin(object sender, JoinEventArgs e)
        {
            // this is how we know the server has successfully joined us to a channel
            if (e.Who == IRC.Nickname 
                && !channelWindows.Any<ChannelWindow>(delegate(ChannelWindow chan) 
                { if (chan.Channel == e.Channel) return true; else return false; }))
            {
                Application.Current.Dispatcher.Invoke(new VoidDelegate(delegate
                {
                    ChannelWindow newWindow = new ChannelWindow(this);
                    newWindow.Channel = e.Channel;
                    newWindow.UserInput += ParseUserInput;

                    newWindow.Closed += channelWindow_Closed;
                    channelWindows.Add(newWindow);
                    newWindow.Show();
                }));
            }
        }

        void IRC_OnRawMessage(object sender, IrcEventArgs e)
        {
            rawMessages.Add(DateTime.Now.ToString("HH:mm:ss.ffff ") + e.Data.RawMessage);
        }

        /// <summary>
        /// Catches the OnNickChange event so we can keep our list of query windows up-to-date.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IRC_OnNickChange(object sender, NickChangeEventArgs e)
        {
            List<string> keysToRemove = new List<string>();
            List<KeyValuePair<string, PrivMsg>> newPrivs = new List<KeyValuePair<string, PrivMsg>>();
            foreach (PrivMsg eachPriv in queryWindows)
                if (eachPriv.QueriedUser == e.OldNickname.ToLower())
                    eachPriv.QueriedUser = e.NewNickname.ToLower();
        }

        #region Private Queries
        void IRC_OnQueryNotice(object sender, IrcEventArgs e)
        {
            DelegateIncomingQueryMessage(e.Data.Nick, e);
        }

        void IRC_OnQueryMessage(object sender, IrcEventArgs e)
        {
            DelegateIncomingQueryMessage(e.Data.Nick, e);
        }

        void IRC_OnQueryAction(object sender, ActionEventArgs e)
        {
            DelegateIncomingQueryMessage(e.Data.Nick, e);
        }

        private void DelegateIncomingQueryMessage(string nick, IrcEventArgs e)
        {
            if (string.IsNullOrEmpty(nick))
                MainOutputWindow.TakeIncomingQueryMessage(e);
            else if (!queryWindows.Any<PrivMsg>(delegate(PrivMsg msg) { return msg.QueriedUser.ToLower() == nick.ToLower(); }))
            {
                if (WindowsForPrivMsgs)
                    Application.Current.Dispatcher.Invoke(new VoidDelegate(delegate { CreateNewPrivWindow(nick, e); }));
                else
                    MainOutputWindow.TakeIncomingQueryMessage(e);
            }
        }

        private void CreateNewPrivWindow(string nick, string message)
        {
            PrivMsg priv = new PrivMsg(this, nick, message);
            SetupNewPrivWindow(priv, nick);
        }

        private void CreateNewPrivWindow(string nick, IrcEventArgs e)
        {
            PrivMsg priv = new PrivMsg(this, nick, e);
            SetupNewPrivWindow(priv, nick);
        }

        private void SetupNewPrivWindow(PrivMsg priv, string nick)
        {
            priv.UserInput += ParseUserInput;
            queryWindows.Add(priv);
            priv.Closed += new EventHandler(Query_Closed);
            priv.Show();
        }

        void Query_Closed(object sender, EventArgs e)
        {
            PrivMsg priv = sender as PrivMsg;
            if (priv != null) priv.Closed -= Query_Closed;

            List<PrivMsg> msgsToRemove = new List<PrivMsg>();
            foreach (PrivMsg msg in queryWindows)
                if (msg == sender)
                    msgsToRemove.Add(msg);
            foreach (PrivMsg s in msgsToRemove)
                queryWindows.Remove(s);

            Window_Closed(sender, e);
        }

        public void SendQueryMessage(string nick, string message)
        {
            PrivMsg match = queryWindows.Single<PrivMsg>(delegate(PrivMsg chan) { return chan.QueriedUser == nick.ToLower(); });
            if (match != null)
                match.TakeOutgoingMessage(message);
            else if (WindowsForPrivMsgs)
                Application.Current.Dispatcher.BeginInvoke(new VoidDelegate(delegate { CreateNewPrivWindow(nick, message); }));
            else
                MainOutputWindow.TakeOutgoingQueryMessage(nick, message);
        }
        #endregion

        void IRC_OnConnected(object sender, EventArgs e)
        {
            IRC.Login(InitialNickname, "Real Name", 0, "username");
            new Thread(new ThreadStart(delegate { IRC.Listen(); })).Start();
        }

        void Window_Closed(object sender, EventArgs e)
        {
            if (Application.Current.Windows.Count == 0)
            {
                if (IRC.IsConnected)
                    IRC.Disconnect();

                Application.Current.Shutdown();
            }
        }

        // Used for storing colors to the conig file
        public static SolidColorBrush CreateBrushFromString(string colorString)
        {
            byte a = 255, r = 255, g = 255, b = 255;
            if (colorString.Length == 7)
            {
                a = 255;
                r = byte.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            }
            else if (colorString.Length == 9)
            {
                a = byte.Parse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                r = byte.Parse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(colorString.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        #region Config File Load/Save
        private void LoadConfigurationFile()
        {
            try
            {
                if (File.Exists(CONFIG_FILE_NAME))
                {
                    string[] configContents = File.ReadAllLines(CONFIG_FILE_NAME);

                    Dictionary<string, string> options = new Dictionary<string, string>();
                    foreach (string line in configContents)
                    {
                        int colonPlace = line.IndexOf(':');
                        string optionName = line.Substring(0, colonPlace);
                        options.Add(optionName, line.Substring(colonPlace + 1, line.Length - colonPlace - 1));
                    }

                    if (options.ContainsKey("FirstChannel"))
                        FirstChannel = options["FirstChannel"];

                    if (options.ContainsKey("Nickname"))
                        InitialNickname = options["Nickname"];
                    if (options.ContainsKey("Server"))
                        Server = options["Server"];
                    if (options.ContainsKey("ServerPort"))
                    {
                        try { ServerPort = int.Parse(options["ServerPort"]); }
                        catch { ServerPort = 6667; }
                    }
                    if (options.ContainsKey("FirstChannelKey"))
                        FirstChannelKey = options["FirstChannelKey"];

                    if (options.ContainsKey("SaveConnectionInfo"))
                    {
                        if (options["SaveConnectionInfo"] == "yes") SaveConnectionInfo = true;
                        else SaveConnectionInfo = false;
                    }

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

                    if (options.ContainsKey("UsersBack")) UsersBack = CreateBrushFromString(options["UsersBack"]);
                    if (options.ContainsKey("UsersFore")) UsersFore = CreateBrushFromString(options["UsersFore"]);
                    if (options.ContainsKey("EntryBack")) EntryBack = CreateBrushFromString(options["EntryBack"]);
                    if (options.ContainsKey("EntryFore")) EntryFore = CreateBrushFromString(options["EntryFore"]);
                    if (options.ContainsKey("ChatBack")) ChatBack = CreateBrushFromString(options["ChatBack"]);
                    if (options.ContainsKey("TimeFore")) TimeFore = CreateBrushFromString(options["TimeFore"]);
                    if (options.ContainsKey("NickFore")) NickFore = CreateBrushFromString(options["NickFore"]);
                    if (options.ContainsKey("BracketFore")) BracketFore = CreateBrushFromString(options["BracketFore"]);
                    if (options.ContainsKey("TextFore")) TextFore = CreateBrushFromString(options["TextFore"]);
                    if (options.ContainsKey("OwnNickFore")) OwnNickFore = CreateBrushFromString(options["OwnNickFore"]);
                    if (options.ContainsKey("LinkFore")) LinkFore = CreateBrushFromString(options["LinkFore"]);

                    if (options.ContainsKey("Font"))
                        Font = new FontFamily(options["Font"]);

                    if (options.ContainsKey("TimestampFormat"))
                        TimeStampFormat = options["TimestampFormat"];

                    if (options.ContainsKey("QueryTextFore"))
                        QueryTextFore = CreateBrushFromString(options["QueryTextFore"]);

                    if (options.ContainsKey("WindowsForPrivMsgs"))
                    {
                        if (options["WindowsForPrivMsgs"] == "yes") WindowsForPrivMsgs = true;
                        else WindowsForPrivMsgs = false;
                    }

                    if (options.ContainsKey("LastFMUserName"))
                        LastFMUserName = options["LastFMUserName"];
                }
            }
            catch (Exception ex)
            {
                Error.ShowError(new Exception("There was an error reading the configuration file", ex));
            }
        }

        private void SaveConfigurationFile()
        {
            StringBuilder options = new StringBuilder();

            if (SaveConnectionInfo)
            {
                options.AppendLine("FirstChannel:" + FirstChannel);
                options.AppendLine("Nickname:" + InitialNickname);
                options.AppendLine("Server:" + Server);
                options.AppendLine("ServerPort:" + ServerPort);
                options.AppendLine("FirstChannelKey:" + FirstChannelKey);
            }
            options.AppendLine("SaveConnectionInfo:" + ((SaveConnectionInfo == true) ? "yes" : "no"));
            options.AppendLine("ClickRestoreType:" + ((RestoreType == ClickRestoreType.SingleClick) ? "single" : "double"));
            options.AppendLine("HighlightTrayForJoinQuits:" + ((HighlightTrayIconForJoinsAndQuits == true) ? "yes" : "no"));
            options.AppendLine("UsersBack:" + UsersBack.Color.ToString());
            options.AppendLine("UsersFore:" + UsersFore.Color.ToString());
            options.AppendLine("EntryBack:" + EntryBack.Color.ToString());
            options.AppendLine("EntryFore:" + EntryFore.Color.ToString());
            options.AppendLine("ChatBack:" + ChatBack.Color.ToString());
            options.AppendLine("TimeFore:" + TimeFore.Color.ToString());
            options.AppendLine("NickFore:" + NickFore.Color.ToString());
            options.AppendLine("BracketFore:" + BracketFore.Color.ToString());
            options.AppendLine("TextFore:" + TextFore.Color.ToString());
            options.AppendLine("QueryTextFore:" + QueryTextFore.Color.ToString());
            options.AppendLine("OwnNickFore:" + OwnNickFore.Color.ToString());
            options.AppendLine("LinkFore:" + LinkFore.Color.ToString());
            options.AppendLine("Font:" + Font.Source);
            options.AppendLine("TimestampFormat:" + TimeStampFormat);
            options.AppendLine("WindowsForPrivMsgs:" + ((WindowsForPrivMsgs == true) ? "yes" : "no"));
            options.AppendLine("LastFMUserName:" + LastFMUserName);

            File.WriteAllText(CONFIG_FILE_NAME, options.ToString());
        }
        #endregion

        private bool ShowConnectionWindow(ChannelWindow FirstWindow)
        {
            ConnectionWindow connWin = new ConnectionWindow(this);
            connWin.Owner = FirstWindow;
            if (connWin.ShowDialog().Value)
            {
                FirstChannel = connWin.Channel;
                InitialNickname = connWin.Nickname;
                Server = connWin.Server;
                FirstChannelKey = connWin.ChannelKey;
            }
            else
            {
                Application.Current.Shutdown();
            }

            return connWin.DialogResult.Value;
        }

        public void ShowOptions()
        {
            //Application.ResourceAssembly
            Options optionsDialog = new Options(this);
            if (optionsDialog.ShowDialog().Value)
                SaveConfigurationFile();
        }

        void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
                if (ex is TargetInvocationException)
                    Error.ShowError(ex.InnerException);
                else
                    Error.ShowError(ex);
        }

        void UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception is TargetInvocationException)
                Error.ShowError(e.Exception.InnerException);
            else
                Error.ShowError(e.Exception);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        void channelWindow_Closed(object sender, EventArgs e)
        {
            ChannelWindow chan = sender as ChannelWindow;
            if (chan != null) chan.Closed -= channelWindow_Closed;

            // if the closing window was our "main" window, we need to choose a new "main" window
            if (sender == MainOutputWindow && channelWindows.Count > 1)
                foreach (ChannelWindow eachChan in channelWindows)
                    if (eachChan != MainOutputWindow)
                    {
                        MainOutputWindow = eachChan;
                        MainOutputWindow.IsMainWindow = true;
                        continue;
                    }

            List<string> keysToRemove = new List<string>();
            foreach (ChannelWindow eachChan in channelWindows)
                if (eachChan == sender)
                    keysToRemove.Add(eachChan.Channel);
            foreach (string s in keysToRemove)
                channelWindows.Remove(channelWindows.Single<ChannelWindow>(delegate(ChannelWindow ch){return ch.Channel == s;}));

            Window_Closed(sender, e);
        }

        public delegate void InputHandler(ChatWindow window, string target, string input, InputEventArgs e);
        public event InputHandler Input;

        public class InputEventArgs
        {
            public bool Handled { get; set; }
        }

        protected void ParseUserInput(ChatWindow sender, string input)
        {
            try
            {
                string target = (sender is ChannelWindow) ? (sender as ChannelWindow).Channel : (sender as PrivMsg).QueriedUser;

                if (Input != null)
                {
                    InputEventArgs e = new InputEventArgs();
                    Input(sender, target, input, e);
                    if (e.Handled)
                        return;
                }

                string[] words = input.Split(' ');

                if (input.Equals("/clear", StringComparison.CurrentCultureIgnoreCase))
                    sender.Clear();
                else if (input.Equals("/options", StringComparison.CurrentCultureIgnoreCase))
                {
                    ShowOptions();
                }
                else if (words[0].Equals("/me", StringComparison.CurrentCultureIgnoreCase))
                {
                    string action;
                    if (input.Length >= 5)
                        action = input.Substring(4);
                    else
                        action = "";
                    IRC.SendMessage(SendType.Action, target, action);

                    sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "* "),
                                                        new ColorTextPair(TextFore, IRC.Nickname) },
                                  new ColorTextPair[] { new ColorTextPair(TextFore, action) });
                }
                else if (words[0].Equals("/nick", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (words.Length == 2)
                        IRC.RfcNick(words[1]);
                    else
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/me <newName>'.  Names may not contain spaces.") });
                }
                else if (words[0].Equals("/op", StringComparison.CurrentCultureIgnoreCase))
                {
                    ChannelWindow channel = sender as ChannelWindow;
                    if (channel != null)
                    {

                    }
                }
                else if (words[0].Equals("/topic", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (sender is PrivMsg)
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "Cannot set topic in a private chat") });
                    else
                    {
                        if (input.Length >= 8)
                            IRC.RfcTopic(target, input.Substring(7));
                        else
                            sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                          new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/topic <new topic>'.") });
                    }
                }
                else if (words[0].Equals("/raw", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (input.Length >= 6)
                        IRC.WriteLine(input.Substring(5));
                    else
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                               new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/raw <raw IRC message>'.") });
                }
                else if (words[0].Equals("/msg", StringComparison.CurrentCultureIgnoreCase))
                {
                    bool syntaxError = false;
                    if (words.Length >= 2 && !string.IsNullOrEmpty(words[1]))
                    {
                        string otherTarget = words[1];
                        if (words.Length >= 3)
                        {
                            string msgText = input.Substring(input.IndexOf(" " + words[1] + " ") + words[1].Length + 2);
                            SendQueryMessage(otherTarget, msgText);
                        }
                        else syntaxError = true;
                    }
                    else syntaxError = true;

                    if (syntaxError)
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
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
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "You must choose a Last.fm username on the options dialog.") });
                    else
                    {
                        WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
                        client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(LastFMdownloadComplete);
                        client.DownloadStringAsync(new Uri("http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=" + LastFMUserName + "&api_key=638e9e076d239d8202be0387769d1da9&limit=1"), sender);
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
                        IRC.RfcJoin(channel, channelKey);
                    else
                        sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "command syntax is '/join <channelName>'.  Names may not contain spaces.") });
                }
                else if (input.Equals("/pyc"))
                {
                    pythonConsole.Show();
                }
                else if (input.StartsWith("/"))
                {
                    sender.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "   Error:") },
                                  new ColorTextPair[] { new ColorTextPair(TextFore, "command not recognized.") });
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    IRC.SendMessage(SendType.Message, target, input);

                    sender.Output(new ColorTextPair[] { new ColorTextPair(BracketFore, "<"),
                                                        new ColorTextPair(OwnNickFore, IRC.Nickname),
                                                        new ColorTextPair(BracketFore, ">") },
                                  new ColorTextPair[] { new ColorTextPair(TextFore, input) });
                }
            }
            catch (Exception ex)
            {
                Error.ShowError(ex);
            }
        }

        private void LastFMdownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            ChatWindow window = e.UserState as ChatWindow;

            try
            {
                string target = (window is ChannelWindow) ? (window as ChannelWindow).Channel : (window as PrivMsg).QueriedUser;

                XDocument doc = XDocument.Parse(e.Result);

                var tracks = from results in doc.Descendants("track")
                             select new { name = results.Element("name").Value, artist = results.Element("artist").Value, date = (DateTime)results.Element("date") };

                foreach (var track in tracks)
                {
                    if (DateTime.Now.Subtract(track.date).Minutes > 30)
                    {
                        window.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, "You haven't submitted a song to Last.fm in the last 30 minutes.  Maybe Last.fm submission service is down?") });
                    }
                    else
                    {
                        string action = "is listening to " + track.name + " by " + track.artist;
                        IRC.SendMessage(SendType.Action, target, action);

                        window.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "* "),
                                                            new ColorTextPair(TextFore, IRC.Nickname) },
                                      new ColorTextPair[] { new ColorTextPair(TextFore, action) });
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                window.Output(new ColorTextPair[] { new ColorTextPair(TextFore, "!") },
                              new ColorTextPair[] { new ColorTextPair(TextFore, ex.Message) });
            }
        }
    }
}
