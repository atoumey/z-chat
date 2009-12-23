using System.Windows;
using System.Windows.Input;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        public string Channel;
        public string Nickname;
        public string Server;
        public int ServerPort;
        public string ChannelKey;

        public ConnectionWindow(App app)
        {
            Channel = app.FirstChannel;
            Nickname = app.InitialNickname;
            Server = app.Server;
            ServerPort = app.ServerPort;
            ChannelKey = app.FirstChannelKey;

            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            channelBox.Text = Channel;
            channelKeyBox.Text = ChannelKey;
            nickNameBox.Text = Nickname;
            serverBox.Text = Server;
            serverPortBox.Text = ServerPort.ToString();
            
            channelBox.Focus();
            channelBox.SelectAll();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Connect();
        }

        private void Connect()
        {
            Channel = channelBox.Text;
            Nickname = nickNameBox.Text;
            Server = serverBox.Text;
            try { ServerPort = int.Parse(serverPortBox.Text); }
            catch { ServerPort = 6667; }
            ChannelKey = channelKeyBox.Text;

            DialogResult = true;
            Close();
        }
    }
}
