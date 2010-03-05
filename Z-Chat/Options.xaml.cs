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

            channelTextBox.Text = ZChat.Options.FirstChannel;
            nickNameTextBox.Text = ZChat.Options.InitialNickname;
            serverTextBox.Text = ZChat.Options.Server;
            serverPortTextBox.Text = ZChat.Options.ServerPort.ToString();
            channelKeyTextBox.Text = ZChat.Options.FirstChannelKey;

            saveConnectionInfoCheckBox.IsChecked = ZChat.Options.SaveConnectionInfo;

            if (ZChat.Options.RestoreType == ClickRestoreType.SingleClick)
            {
                singleClickRestore.IsChecked = true;
                doubleClickRestore.IsChecked = false;
            }
            else
            {
                singleClickRestore.IsChecked = false;
                doubleClickRestore.IsChecked = true;
            }

            joinsQuitsHighlight.IsChecked = ZChat.Options.HighlightTrayIconForJoinsAndQuits;

            UsersBack.Background = ZChat.Options.UsersBack;
            UsersFore.Background = ZChat.Options.UsersFore;
            EntryBack.Background = ZChat.Options.EntryBack;
            EntryFore.Background = ZChat.Options.EntryFore;
            ChatBack.Background = ZChat.Options.ChatBack;
            TimeFore.Background = ZChat.Options.TimeFore;
            NickFore.Background = ZChat.Options.NickFore;
            BracketFore.Background = ZChat.Options.BracketFore;
            TextFore.Background = ZChat.Options.TextFore;
            QueryTextFore.Background = ZChat.Options.QueryTextFore;
            OwnNickFore.Background = ZChat.Options.OwnNickFore;
            LinkFore.Background = ZChat.Options.LinkFore;

            fontsCombo.ItemsSource = Fonts.SystemFontFamilies;
            fontsCombo.SelectedValue = ZChat.Options.Font.Source;

            timeFormatBox.Text = ZChat.Options.TimeStampFormat;
            windowsForPrivMsgs.IsChecked = ZChat.Options.WindowsForPrivMsgs;
            lastfmUserBox.Text = ZChat.Options.LastFMUserName;
            hyperlinkPatternBox.Text = ZChat.Options.HyperlinkPattern;

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
            ZChat.Options.FirstChannel = channelTextBox.Text;
            ZChat.Options.InitialNickname = nickNameTextBox.Text;
            ZChat.Options.Server = serverTextBox.Text;
            try { ZChat.Options.ServerPort = int.Parse(serverPortTextBox.Text); }
            catch { serverPortTextBox.Text = ZChat.Options.ServerPort.ToString(); }
            ZChat.Options.FirstChannelKey = channelKeyTextBox.Text;

            ZChat.Options.SaveConnectionInfo = saveConnectionInfoCheckBox.IsChecked.Value;

            if (singleClickRestore.IsChecked.Value)
                ZChat.Options.RestoreType = ClickRestoreType.SingleClick;
            else
                ZChat.Options.RestoreType = ClickRestoreType.DoubleClick;

            ZChat.Options.HighlightTrayIconForJoinsAndQuits = joinsQuitsHighlight.IsChecked.Value;

            ZChat.Options.UsersBack = (SolidColorBrush)UsersBack.Background;
            ZChat.Options.UsersFore = (SolidColorBrush)UsersFore.Background;
            ZChat.Options.EntryBack = (SolidColorBrush)EntryBack.Background;
            ZChat.Options.EntryFore = (SolidColorBrush)EntryFore.Background;
            ZChat.Options.ChatBack = (SolidColorBrush)ChatBack.Background;
            ZChat.Options.TimeFore = (SolidColorBrush)TimeFore.Background;
            ZChat.Options.NickFore = (SolidColorBrush)NickFore.Background;
            ZChat.Options.BracketFore = (SolidColorBrush)BracketFore.Background;
            ZChat.Options.TextFore = (SolidColorBrush)TextFore.Background;
            ZChat.Options.QueryTextFore = (SolidColorBrush)QueryTextFore.Background;
            ZChat.Options.OwnNickFore = (SolidColorBrush)OwnNickFore.Background;
            ZChat.Options.LinkFore = (SolidColorBrush)LinkFore.Background;

            ZChat.Options.Font = (FontFamily)fontsCombo.SelectedItem;

            ZChat.Options.TimeStampFormat = timeFormatBox.Text;
            ZChat.Options.WindowsForPrivMsgs = windowsForPrivMsgs.IsChecked.Value;
            ZChat.Options.LastFMUserName = lastfmUserBox.Text;
            ZChat.Options.HyperlinkPattern = hyperlinkPatternBox.Text;
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
