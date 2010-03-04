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
        public Chat ZChat { get { return _zchat; } set { _zchat = value; FirePropertyChanged("ZChat"); } }
        private Chat _zchat;

        public Options(Chat parent) : base()
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("ZChat.IRC.ico"));
            ZChat = parent;
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
    }
}
