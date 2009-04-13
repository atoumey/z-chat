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
using System.Text;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        public App ZChat;

        public Options(App parent)
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("ZChat.IRC.ico"));
            ZChat = parent;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveOptions()
        {
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

            SaveConfigurationFile();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SaveOptions();
            Close();
        }

        private void SaveConfigurationFile()
        {
            StringBuilder options = new StringBuilder();

            options.AppendLine("ClickRestoreType:" + ((ZChat.RestoreType == ClickRestoreType.SingleClick) ? "single" : "double"));
            options.AppendLine("HighlightTrayForJoinQuits:" + ((ZChat.HighlightTrayIconForJoinsAndQuits == true) ? "yes" : "no"));
            options.AppendLine("UsersBack:" + ZChat.UsersBack.Color.ToString());
            options.AppendLine("UsersFore:" + ZChat.UsersFore.Color.ToString());
            options.AppendLine("EntryBack:" + ZChat.EntryBack.Color.ToString());
            options.AppendLine("EntryFore:" + ZChat.EntryFore.Color.ToString());
            options.AppendLine("ChatBack:" + ZChat.ChatBack.Color.ToString());
            options.AppendLine("TimeFore:" + ZChat.TimeFore.Color.ToString());
            options.AppendLine("NickFore:" + ZChat.NickFore.Color.ToString());
            options.AppendLine("BracketFore:" + ZChat.BracketFore.Color.ToString());
            options.AppendLine("TextFore:" + ZChat.TextFore.Color.ToString());
            options.AppendLine("QueryTextFore:" + ZChat.QueryTextFore.Color.ToString());
            options.AppendLine("OwnNickFore:" + ZChat.OwnNickFore.Color.ToString());
            options.AppendLine("LinkFore:" + ZChat.LinkFore.Color.ToString());
            options.AppendLine("Font:" + ZChat.Font.Source);
            options.AppendLine("TimestampFormat:" + ZChat.TimeStampFormat);
            options.AppendLine("WindowsForPrivMsgs:" + ((ZChat.WindowsForPrivMsgs == true) ? "yes" : "no"));
            options.AppendLine("LastFMUserName:" + ZChat.LastFMUserName);

            File.WriteAllText(App.CONFIG_FILE_NAME, options.ToString());
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
