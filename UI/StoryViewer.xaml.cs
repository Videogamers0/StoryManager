using StoryManager.VM.Helpers;
using StoryManager.VM;
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
    /// Interaction logic for StoryViewer.xaml
    /// </summary>
    public partial class StoryViewer : UserControl
    {
        public StoryViewer()
        {
            InitializeComponent();

            //  Force the WebView2 instance to initialize even though it may be hosted on an inactive tab inside a TabControl
            //  Seealso: https://dev.to/timothymcgrath/til-how-to-fix-webview2-in-wpf-tabcontrol-5g8o
            Loaded += (_, __) =>
            {
                //  Traverse up the visual tree to find the TabControl that the WebView2 belongs to
                //  then temporarily activate the TabItem that the WebView2 instance is hosted in
                //  This does not work for TabControls nested within TabControls, would need to activate every tab of every tabcontrol along the visual tree but I don't care
                DependencyObject CurrentItem = WebView.Parent;
                TabItem WebViewTab = null;
                while (CurrentItem != null)
                {
                    if (CurrentItem is TabItem TabItem)
                    {
                        WebViewTab = TabItem;
                        CurrentItem = TabItem.Parent;
                    }
                    else if (CurrentItem is TabControl TC)
                    {
                        var PreviouslySelected = TC.SelectedItem;
                        TC.SelectedItem = WebViewTab;
                        TC.UpdateLayout();
                        TC.SelectedItem = PreviouslySelected;
                        break;
                    }
                    else if (CurrentItem is FrameworkElement FWE)
                        CurrentItem = FWE.Parent;
                    else
                        break;
                }
            };
        }

        private void OpenSelectedStoryInBrowser(object sender, RequestNavigateEventArgs e)
        {
            if (DataContext is MainViewModel MVM && MVM.SelectedStory != null)
            {
                GeneralUtils.OpenUrl(MVM.SelectedStory.Summary.Chapters.First().FullUrl, true);
            }
        }
    }
}
