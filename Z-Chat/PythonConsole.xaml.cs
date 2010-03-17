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
using System.IO;
using Microsoft.Scripting.Hosting;
using System.ComponentModel;
using Microsoft.Scripting;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for PythonConsole.xaml
    /// </summary>
    public partial class PythonConsole : Window, INotifyPropertyChanged
    {
        public Chat ZChat { get { return _zchat; } set { _zchat = value; FirePropertyChanged("ZChat"); } }
        private Chat _zchat;

        private ScriptScope _pythonScope;
        
        public TextBox PythonOutput { get { return consoleOutput; } }

        public PythonConsole(Chat zchat)
        {
            InitializeComponent();
            Closing += new CancelEventHandler(PythonConsole_Closing);

            ZChat = zchat;
            EntryHistory.Add("");
            
            _pythonScope = ZChat.PythonEngine.CreateScope();
            _pythonScope.SetVariable("zchat", ZChat);
        }

        void PythonConsole_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            ZChat.ShutdownIfReady();
        }

        public int NextHistoricalEntry;
        public List<string> EntryHistory = new List<string>();

        StringBuilder input = new StringBuilder(255);
        private void consoleInput_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Up)
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

        //void writePythonOutputToScreen()
        //{
        //    int length = (int)_pythonOutput.Length;
        //    if (length > 0)
        //    {
        //        byte[] bytes = new byte[length];

        //        _pythonOutput.Seek(0, SeekOrigin.Begin);
        //        _pythonOutput.Read(bytes, 0, length);

        //        consoleOutput.AppendText(Encoding.UTF8.GetString(bytes, 0, length));
        //        _pythonOutput.SetLength(0);
        //    }
        //}

        bool allow_incomplete(string[] lines)
        {
            if (string.IsNullOrEmpty(lines[lines.Length - 1]))
            {
                lines[lines.Length - 1] = "";
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
                //writePythonOutputToScreen();
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
            //writePythonOutputToScreen();
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
            foreach (string line in traceback.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void FirePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            consoleOutput.Clear();
        }

        private void ResetScope_Click(object sender, RoutedEventArgs e)
        {
            _pythonScope = ZChat.PythonEngine.CreateScope();
            _pythonScope.SetVariable("zchat", ZChat);
        }

        private void consoleInput_KeyDown(object sender, KeyEventArgs e)
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
        }
    }
}
