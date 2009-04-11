using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using System.Diagnostics;
using System;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public ChannelWindow ChatWindow;

        public Options(ChannelWindow parent)
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("ZChat.IRC.ico"));
            ChatWindow = parent;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ChatWindow.RestoreType == ZChat.ActivityWindow.ClickRestoreType.SingleClick)
            {
                singleClickRestore.IsChecked = true;
                doubleClickRestore.IsChecked = false;
            }
            else
            {
                singleClickRestore.IsChecked = false;
                doubleClickRestore.IsChecked = true;
            }

            joinsQuitsHighlight.IsChecked = ChatWindow.HighlightTrayIconForJoinsAndQuits;

            UsersBack.Background = ChatWindow.UsersBack;
            UsersFore.Background = ChatWindow.UsersFore;
            EntryBack.Background = ChatWindow.EntryBack;
            EntryFore.Background = ChatWindow.EntryFore;
            ChatBack.Background = ChatWindow.ChatBack;
            TimeFore.Background = ChatWindow.TimeFore;
            NickFore.Background = ChatWindow.NickFore;
            BracketFore.Background = ChatWindow.BracketFore;
            TextFore.Background = ChatWindow.TextFore;
            QueryTextFore.Background = ChatWindow.QueryTextFore;
            OwnNickFore.Background = ChatWindow.OwnNickFore;
            LinkFore.Background = ChatWindow.LinkFore;

            fontsCombo.ItemsSource = Fonts.SystemFontFamilies;
            fontsCombo.SelectedValue = ChatWindow.Font.Source;

            timeFormatBox.Text = ChatWindow.TimeStampFormat;
            windowsForPrivMsgs.IsChecked = ChatWindow.WindowsForPrivMsgs;
            lastfmUserBox.Text = ChatWindow.LastFMUserName;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveOptions()
        {
            if (singleClickRestore.IsChecked.Value)
                ChatWindow.RestoreType = ZChat.ActivityWindow.ClickRestoreType.SingleClick;
            else
                ChatWindow.RestoreType = ZChat.ActivityWindow.ClickRestoreType.DoubleClick;

            ChatWindow.HighlightTrayIconForJoinsAndQuits = joinsQuitsHighlight.IsChecked.Value;

            ChatWindow.UsersBack = (SolidColorBrush)UsersBack.Background;
            ChatWindow.UsersFore = (SolidColorBrush)UsersFore.Background;
            ChatWindow.EntryBack = (SolidColorBrush)EntryBack.Background;
            ChatWindow.EntryFore = (SolidColorBrush)EntryFore.Background;
            ChatWindow.ChatBack = (SolidColorBrush)ChatBack.Background;
            ChatWindow.TimeFore = (SolidColorBrush)TimeFore.Background;
            ChatWindow.NickFore = (SolidColorBrush)NickFore.Background;
            ChatWindow.BracketFore = (SolidColorBrush)BracketFore.Background;
            ChatWindow.TextFore = (SolidColorBrush)TextFore.Background;
            ChatWindow.QueryTextFore = (SolidColorBrush)QueryTextFore.Background;
            ChatWindow.OwnNickFore = (SolidColorBrush)OwnNickFore.Background;
            ChatWindow.LinkFore = (SolidColorBrush)LinkFore.Background;

            ChatWindow.Font = (FontFamily)fontsCombo.SelectedItem;

            ChatWindow.TimeStampFormat = timeFormatBox.Text;
            ChatWindow.WindowsForPrivMsgs = windowsForPrivMsgs.IsChecked.Value;
            ChatWindow.LastFMUserName = lastfmUserBox.Text;

            SaveConfigurationFile();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            Close();
        }

        private void SaveConfigurationFile()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User
                | IsolatedStorageScope.Assembly, null, null);

            IsolatedStorageFileStream oStream = new IsolatedStorageFileStream(ChannelWindow.ISOLATED_FILE_NAME,
                FileMode.Create, isoStore);

            StreamWriter writer = new StreamWriter(oStream);
            writer.WriteLine("ClickRestoreType:" + ((ChatWindow.RestoreType == ZChat.ActivityWindow.ClickRestoreType.SingleClick) ? "single" : "double"));
            writer.WriteLine("HighlightTrayForJoinQuits:" + ((ChatWindow.HighlightTrayIconForJoinsAndQuits == true) ? "yes" : "no"));
            writer.WriteLine("UsersBack:" + ChatWindow.UsersBack.Color.ToString());
            writer.WriteLine("UsersFore:" + ChatWindow.UsersFore.Color.ToString());
            writer.WriteLine("EntryBack:" + ChatWindow.EntryBack.Color.ToString());
            writer.WriteLine("EntryFore:" + ChatWindow.EntryFore.Color.ToString());
            writer.WriteLine("ChatBack:" + ChatWindow.ChatBack.Color.ToString());
            writer.WriteLine("TimeFore:" + ChatWindow.TimeFore.Color.ToString());
            writer.WriteLine("NickFore:" + ChatWindow.NickFore.Color.ToString());
            writer.WriteLine("BracketFore:" + ChatWindow.BracketFore.Color.ToString());
            writer.WriteLine("TextFore:" + ChatWindow.TextFore.Color.ToString());
            writer.WriteLine("QueryTextFore:" + ChatWindow.QueryTextFore.Color.ToString());
            writer.WriteLine("OwnNickFore:" + ChatWindow.OwnNickFore.Color.ToString());
            writer.WriteLine("LinkFore:" + ChatWindow.LinkFore.Color.ToString());
            writer.WriteLine("Font:" + ChatWindow.Font.Source);
            writer.WriteLine("TimestampFormat:" + ChatWindow.TimeStampFormat);
            writer.WriteLine("WindowsForPrivMsgs:" + ((ChatWindow.WindowsForPrivMsgs == true) ? "yes" : "no"));
            writer.WriteLine("LastFMUserName:" + ChatWindow.LastFMUserName);
            writer.Close();

            oStream.Close();
            isoStore.Close();
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
                SaveOptions();
                Close();
            }
        }
    }
}
