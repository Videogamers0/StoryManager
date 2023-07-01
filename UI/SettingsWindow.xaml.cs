using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using StoryManager.VM;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StoryManager.UI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(Settings DataContext)
        {
            InitializeComponent();
            this.DataContext = DataContext;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
