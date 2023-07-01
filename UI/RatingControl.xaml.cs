using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StoryManager.UI
{
    //Taken from: http://dotnetvisio.blogspot.com/2014/02/create-5-star-rating-control-in-xaml.html

    /// <summary>
    /// Interaction logic for RatingControl.xaml
    /// </summary>
    public partial class RatingControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(RatingControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(RatingChanged)));

        private int _max = 5;

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Clamp(value, 0, _max));
        }

        private static void RatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            RatingControl item = sender as RatingControl;
            int newval = (int)e.NewValue;
            UIElementCollection childs = ((Grid)(item.Content)).Children;

            ToggleButton button = null;

            for (int i = 0; i < newval; i++)
            {
                button = childs[i] as ToggleButton;
                if (button != null)
                    button.IsChecked = true;
            }

            for (int i = newval; i < childs.Count; i++)
            {
                button = childs[i] as ToggleButton;
                if (button != null)
                    button.IsChecked = false;
            }

        }

        private void ClickEventHandler(object sender, RoutedEventArgs args)
        {
            ToggleButton button = sender as ToggleButton;
            int newvalue = int.Parse(button.Tag.ToString());
            Value = newvalue;
        }

        public RatingControl()
        {
            InitializeComponent();
        }
    }
}