using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Policy;
using System.Collections.ObjectModel;
using System.Web;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Diagnostics;
using Microsoft.Web.WebView2.Wpf;
using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
using StoryManager.VM;
using System.Windows.Navigation;

namespace StoryManager.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                try { DataContext = new MainViewModel(this); }
                catch (Exception ex) { MessageBox.Show($"Error during initialization:\n\n{ex.ToString()}"); }
            };
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hl)
                GeneralUtils.OpenUrl(hl.NavigateUri.ToString(), false);
        }

        private double? PreviousSearchRowHeight = null;
        private void SearchExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (SearchRow.Height.IsAuto)
            {
                if (!PreviousSearchRowHeight.HasValue)
                    SearchRow.Height = new GridLength(338, GridUnitType.Pixel);
                else
                    SearchRow.Height = new GridLength(PreviousSearchRowHeight.Value, GridUnitType.Pixel);
            }
        }
        private void SearchExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            PreviousSearchRowHeight = SearchRow.ActualHeight;
            SearchRow.Height = GridLength.Auto;
        }
    }
}
