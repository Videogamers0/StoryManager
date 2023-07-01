using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
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
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public DownloadWindow(Downloader DataContext)
        {
            InitializeComponent();
            this.DataContext = DataContext;

#if NEVER //DEBUG
            this.TopStoriesButton.Visibility = Visibility.Visible;
#endif
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hl)
                GeneralUtils.OpenUrl(hl.NavigateUri.ToString(), true);
        }
    }
}
