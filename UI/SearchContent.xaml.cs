using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    /// Interaction logic for SearchContent.xaml
    /// </summary>
    public partial class SearchContent : UserControl
    {
        public SearchContent()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                //  Scroll to top of search results when navigating to a new page or committing a new search query
                DependencyPropertyDescriptor ItemsSourceProperty = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
                ItemsSourceProperty?.AddValueChanged(ResultsList, (_, _) => ResultsScroller.ScrollToTop());
            };
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink hl)
                GeneralUtils.OpenUrl(hl.NavigateUri.ToString(), true);
        }

        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is ItemsControl itemsControl && GetItemsPanel(itemsControl) is WrapPanel wp)
            {
                //  Size the items of the WrapPanel to be as wide as possible while still fitting the maximum # of columns into the WrapPanel (each column must be at least 450px wide)
                //  EX: If total width = 899, we can only fit 1 450px column. ItemWidth = 899
                //      But if total width increased to 904, we can now fit 2 450px columns. ItemWidth = 904/2=452
                const int MinimumItemWidth = 450;
                double AvailableWidth = (int)Math.Min(itemsControl.ActualWidth, wp.ActualWidth);
                int Columns = (int)Math.Floor(AvailableWidth / MinimumItemWidth);
                wp.ItemWidth = (int)(AvailableWidth / Columns);
            }
        }

        #region https://stackoverflow.com/a/4744947
        private static Panel GetItemsPanel(DependencyObject itemsControl)
        {
            ItemsPresenter itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
            Panel itemsPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as Panel;
            return itemsPanel;
        }

        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
        #endregion
    }
}
