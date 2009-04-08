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
using System.Reflection;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error : Window
    {
        public Exception Exception;

        public Error(Exception exception)
        {
            InitializeComponent();

            Icon = BitmapFrame.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("ZChat.IRC.ico"));
            Exception = exception;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            messageTextBlock.Text = Exception.Message;
            
            StringBuilder sb = new StringBuilder();
            sb.Append(Exception.StackTrace);
            Exception innerException = Exception.InnerException;
            while (innerException != null)
            {
                sb.Append(System.Environment.NewLine);
                sb.Append(System.Environment.NewLine);
                sb.Append("Message: " + innerException.Message);
                sb.Append(System.Environment.NewLine);
                sb.Append("Stack Trace:");
                sb.Append(System.Environment.NewLine);
                sb.Append(innerException.StackTrace);
                innerException = innerException.InnerException;
            }
            stackTraceTextBox.Text = sb.ToString();
        }

        public static void ShowError(Exception exception)
        {
            Error errorForm = new Error(exception);
            errorForm.ShowDialog();
        }
    }
}
