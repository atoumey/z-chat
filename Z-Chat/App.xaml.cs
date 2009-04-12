using System;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.IO;
using Meebey.SmartIrc4net;
using System.Threading;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string FirstChannel;
        private string Nickname;
        private string Server;

        public IrcClient IRC = new IrcClient();

        public App()
        {
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(UnhandledException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainUnhandledException);

            IRC.ActiveChannelSyncing = true;
            IRC.AutoNickHandling = true;
            IRC.SupportNonRfc = true;
            IRC.Encoding = System.Text.Encoding.UTF8;
            IRC.OnConnected += new EventHandler(IRC_OnConnected);

            ChannelWindow firstWindow = new ChannelWindow(this);
            firstWindow.Closed += new EventHandler(Window_Closed);
            firstWindow.Show();

            if (!File.Exists("config.txt"))
            {
                ShowConnectionWindow(firstWindow);
            }
            else
            {
                ReadConfigFile();
            }

            firstWindow.Channel = FirstChannel;
            IRC.Connect(Server, 6667);
        }

        void IRC_OnConnected(object sender, EventArgs e)
        {
            IRC.Login(Nickname, "Real Name", 0, "username");
            new Thread(new ThreadStart(delegate { IRC.Listen(); })).Start();
        }

        void Window_Closed(object sender, EventArgs e)
        {
            if (this.Windows.Count == 0)
            {
                if (IRC.IsConnected)
                    IRC.Disconnect();

                Shutdown();
            }
        }

        private void ReadConfigFile()
        {
            throw new NotImplementedException();
        }

        private void ShowConnectionWindow(ChannelWindow firstWindow)
        {
            ConnectionWindow connWin = new ConnectionWindow();
            connWin.Owner = firstWindow;
            if (connWin.ShowDialog().Value)
            {
                FirstChannel = connWin.Channel;
                Nickname = connWin.Nickname;
                Server = connWin.Server;
            }
            else
            {
                Shutdown();
            }
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
    }

    delegate void VoidDelegate();
}
