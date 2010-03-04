using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ZChat
{
    public class PluginInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public Version Version { get; set; }
    }

    public abstract class Plugin
    {
        public abstract PluginInfo Info { get; }

        public Plugin() { }

        /// <summary>
        /// Use the App object to subscribe to events
        /// </summary>
        public virtual void Initialize(Chat zchat)
        {
            return;
        }

        /// <summary>
        /// Gets Grids that will each be shown on their own page in the options dialog
        /// </summary>
        public virtual Grid[] GetOptionGrids()
        {
            return new Grid[0];
        }

        /// <summary>
        /// Called when the options dialog is closing and the options are being saved.  Return a string
        /// that will be appended to the zchat_config.txt file, or write your own save logic.
        /// 
        /// If returning anything other than an empty string, store each option on it's own line in the
        /// format Name:Value
        /// </summary>
        /// <returns></returns>
        public virtual string SaveOptions()
        {
            return string.Empty;
        }

        /// <summary>
        /// Called when options are loaded from the zchat_config.txt file.  The options are read from the
        /// file assuming the format Name:Value
        /// </summary>
        public virtual void LoadOptions(KeyValuePair<string, string>[] options)
        {
            return;
        }
    }
}
