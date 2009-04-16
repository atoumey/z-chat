using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace ZChat
{
    /// <summary>
    /// Window that can minimize to the system tray, and can show activity by changing icons on 
    /// the window or the tray.
    /// </summary>
    public class ActivityWindow : Window
    {
        protected App ZChat;

        /// <summary>
        /// Change the state of the window to either show activity or show no activity.
        /// </summary>
        /// <param name="activity">True if you want the window to indicate activity through the icon</param>
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

        public ActivityWindow() { }

        public ActivityWindow(App app)
        {
            ZChat = app;
            ZChat.PropertyChanged += ZChat_PropertyChanged;

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            SetRestoreType();

            notifyClickHandler = new EventHandler(notifyIcon_Click);
            notifyIcon.Click += notifyClickHandler;

            StateChanged += Window_StateChanged;
            IsVisibleChanged += Window_IsVisibleChanged;
            Activated += Window_Activated;
            Closed += Window_Closed;
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

        protected System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Drawing.Icon trayIconNoActivity;
        private System.Drawing.Icon trayIconActivity;

        private BitmapFrame windowIconNoActivity;
        private BitmapFrame windowIconActivity;

        private EventHandler notifyClickHandler;

        private bool balloonShownAlready = false;
        private WindowState storedWindowState = WindowState.Normal;
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
    }

    public enum ClickRestoreType
    {
        SingleClick,
        DoubleClick
    }
}
