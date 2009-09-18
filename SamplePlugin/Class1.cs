using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZChat;
using System.Windows.Controls;
using System.Windows;

namespace SamplePlugin
{
    /// <summary>
    /// A sample plugin that shows an option on the options dialog
    /// </summary>
    public class SamplePlugin : Plugin
    {
        private string SampleOption = "test123";
        private TextBox myTextBox;

        public override PluginInfo Info
        {
            get 
            {
                PluginInfo info = new PluginInfo();
                info.Name = "Sample Plugin";
                info.Author = "Alex";
                info.Version = new Version("0.0.0.1");
                info.Description = "A sample plugin implementation to demonstrate the creation of a simple plugin";
                return info;
            }
        }

        public override System.Windows.Controls.Grid[] GetOptionGrids()
        {
            Grid myGrid = new Grid();
            myGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            myGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            Label label = new Label() { Content = "sample option" };
            label.SetValue(Grid.ColumnProperty, 0);
            myGrid.Children.Add(label);
            myTextBox = new TextBox() { Text = SampleOption, Width = 150 };
            myTextBox.SetValue(Grid.ColumnProperty, 1);
            myGrid.Children.Add(myTextBox);
            return new Grid[] { myGrid };
        }

        public override string SaveOptions()
        {
            return "SampleOption" + ":" + myTextBox.Text;
        }

        public override void LoadOptions(KeyValuePair<string, string>[] options)
        {
            foreach (KeyValuePair<string, string> option in options)
                if (option.Key == "SampleOption")
                    SampleOption = option.Value;
        }
    }
}
