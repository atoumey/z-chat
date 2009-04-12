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

        public ConnectionWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nickNameBox.Text = Environment.UserName;
            channelBox.Focus();
            channelBox.SelectAll();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (channelBox.Text.StartsWith("#"))
                Channel = channelBox.Text;
            else
                Channel = '#' + channelBox.Text;
            Nickname = nickNameBox.Text;
            Server = serverBox.Text;

            DialogResult = true;
            Close();
        }
    }
}
