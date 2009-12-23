using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace ZChat
{
    /// <summary>
    /// Window that can minimize to the system tray, and can show activity by changing icons on 
    /// the window or the tray.  Hosts any number of ChatWindows.
    /// </summary>
    public partial class ActivityWindow : Window
    {
        protected System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Drawing.Icon trayIconNoActivity;
        private System.Drawing.Icon trayIconActivity;

        private BitmapFrame windowIconNoActivity;
        private BitmapFrame windowIconActivity;

        private EventHandler notifyClickHandler;

        private bool balloonShownAlready = false;
        private WindowState storedWindowState = WindowState.Normal;

        protected App ZChat { get; set; }

        public ChatWindow ActiveChat { get; set; }

        public ObservableCollection<ChatWindow> Chats { get { return _chats; } }
        protected ObservableCollection<ChatWindow> _chats = new ObservableCollection<ChatWindow>();

        /// <summary>
        /// Change either the window icon or the tray icon to show activity, depending on 
        /// whether the window is minimized to the system tray.
        /// </summary>
        public void ShowActivity()
        {
            Dispatcher.BeginInvoke(new VoidDelegate(delegate()
            {
                if (WindowState == WindowState.Minimized)
                    notifyIcon.Icon = trayIconActivity;
                else if (!IsActive)
                    Icon = windowIconActivity;
            }));
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Icon = windowIconNoActivity;
        }

        /// <summary>
        /// The name of the icon that will be used for the system tray when no activity is shown.
        /// The icon must be an embedded resource.  The name should be Namespace.Name.ico
        /// </summary>
        protected string TrayIconName_NoActivity
        {
            set
            {
                trayIconNoActivity = new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(value));
                notifyIcon.Icon = trayIconNoActivity;
            }
        }

        /// <summary>
        /// The name of the icon that will be used for the system tray when activity is shown.
        /// The icon must be an embedded resource.  The name should be Namespace.Name.ico
        /// </summary>
        protected string TrayIconName_Activity
        {
            set
            {
                trayIconActivity = new System.Drawing.Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(value));
            }
        }

        /// <summary>
        /// The name of the icon that will be used for the window when no activity is shown.
        /// The icon must be an embedded resource.  The name should be Namespace.Name.ico
        /// </summary>
        protected string WindowIconName_NoActivity
        {
            set
            {
                windowIconNoActivity = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream(value));
                Icon = windowIconNoActivity;
            }
        }

        /// <summary>
        /// The name of the icon that will be used for the window when activity is shown.
        /// The icon must be an embedded resource.  The name should be Namespace.Name.ico
        /// </summary>
        protected string WindowIconName_Activity
        {
            set
            {
                windowIconActivity = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream(value));
            }
        }

        public ActivityWindow()
        {
            InitializeComponent();
        }

        public ActivityWindow(App app) : this()
        {
            ZChat = app;
            ZChat.PropertyChanged += ZChat_PropertyChanged;

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyClickHandler = new EventHandler(notifyIcon_Click);
            SetRestoreType();

            StateChanged += Window_StateChanged;
            IsVisibleChanged += Window_IsVisibleChanged;
            Activated += Window_Activated;
            Closed += Window_Closed;

            Chats.CollectionChanged += Chats_CollectionChanged;
        }

        private void Chats_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (ChatWindow newChat in e.NewItems)
                {
                    Button newChatButton = new Button()
                    {
                        Content = newChat.Title,
                        Tag = newChat
                    };
                    newChatButton.Click += chatButton_Click;
                    buttonPanel.Children.Add(newChatButton);
                }
            if (e.OldItems != null)
                foreach (ChatWindow oldChat in e.OldItems)
                {
                    Button buttonForChat = GetChatButton(oldChat);
                    if (buttonForChat != null)
                    {
                        buttonForChat.Click -= chatButton_Click;
                        buttonPanel.Children.Remove(buttonForChat);
                    }
                }
        }

        private Button GetChatButton(ChatWindow chat)
        {
            foreach (Button button in buttonPanel.Children)
                if (button.Tag == chat)
                    return button;
            return null;
        }

        private void chatButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SetRestoreType()
        {
            if (ZChat.RestoreType == ClickRestoreType.SingleClick)
            {
                notifyIcon.DoubleClick -= notifyClickHandler;
                notifyIcon.Click += notifyClickHandler;
            }
            else if (ZChat.RestoreType == ClickRestoreType.DoubleClick)
            {
                notifyIcon.Click -= notifyClickHandler;
                notifyIcon.DoubleClick += notifyClickHandler;
            }
        }

        void ZChat_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RestoreType")
            {
                SetRestoreType();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ZChat.PropertyChanged -= ZChat_PropertyChanged;

            notifyIcon.Dispose();
            notifyIcon = null;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            else
                storedWindowState = WindowState;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CheckTrayIcon();
        }

        private void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        private void ShowTrayIcon(bool show)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = show;
                notifyIcon.Icon = trayIconNoActivity;
            }
        }

        void notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = storedWindowState;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }
    }

    public enum ClickRestoreType
    {
        SingleClick,
        DoubleClick
    }
}
