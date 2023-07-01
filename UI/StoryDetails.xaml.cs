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
    /// Interaction logic for StoryDetails.xaml
    /// </summary>
    public partial class StoryDetails : UserControl
    {
        public StoryDetails()
        {
            InitializeComponent();
        }

        private void OpenCategoryInBrowser(object sender, RequestNavigateEventArgs e)
        {
            string FullUrl = $"https://www.literotica.com/c/{e.Uri.ToString()}";
            GeneralUtils.OpenUrl(FullUrl, true);
        }
    }
}
