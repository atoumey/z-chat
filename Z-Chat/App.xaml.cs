using System;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.IO;
using Meebey.SmartIrc4net;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;
using System.Net;
using System.Xml.Linq;
using System.Linq;

using IronPython;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using IronPython.Runtime;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Input;

namespace ZChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Chat chat = new Chat();
        }
    }
}
