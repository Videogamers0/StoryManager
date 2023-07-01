using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StoryManager.UI
{
    /// <summary>
    /// Interaction logic for GithubAd.xaml
    /// </summary>
    public partial class GithubAd : Window
    {
        public int OpenedCount { get; }

        public GithubAd(int Count)
        {
            InitializeComponent();

            OpenedCount = Count;

            DataContext = this;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hl)
                GeneralUtils.OpenUrl(hl.NavigateUri.ToString(), false);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
