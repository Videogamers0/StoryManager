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
    /// Interaction logic for StoriesList.xaml
    /// </summary>
    public partial class StoriesList : UserControl
    {
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(StoriesList), new PropertyMetadata(true, HandleIsExpandedChanged));

        private static void HandleIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StoriesList storiesList)
            {
                if (e.NewValue is bool isExpanded)
                {
                    storiesList.OnIsExpandedChanged?.Invoke(storiesList, isExpanded);
                    if (isExpanded)
                        storiesList.OnExpanded?.Invoke(storiesList, EventArgs.Empty);
                    else
                        storiesList.OnCollapsed?.Invoke(storiesList, EventArgs.Empty);
                }
            }
        }

        public event EventHandler<bool> OnIsExpandedChanged;
        public event EventHandler<EventArgs> OnExpanded;
        public event EventHandler<EventArgs> OnCollapsed;

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public StoriesList()
        {
            InitializeComponent();
        }

        private void OpenCategoryInBrowser(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hl && !string.IsNullOrEmpty(hl.NavigateUri.ToString()))
            {
                string FullUrl = $"https://www.literotica.com/c/{e.Uri}";
                GeneralUtils.OpenUrl(FullUrl, true);
            }
        }

        private void SwallowMouseEvent(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
