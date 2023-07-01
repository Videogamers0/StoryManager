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
    /// Interaction logic for FavoritesToggle.xaml
    /// </summary>
    public partial class FavoritesToggle : UserControl
    {
        public static readonly DependencyProperty ShowIfUnfavoritedProperty =
            DependencyProperty.Register(nameof(ShowIfUnfavorited), typeof(bool), typeof(FavoritesToggle), new PropertyMetadata(true));

        public bool ShowIfUnfavorited
        {
            get => (bool)GetValue(ShowIfUnfavoritedProperty);
            set => SetValue(ShowIfUnfavoritedProperty, value);
        }

        public static readonly DependencyProperty IsFavoritedProperty =
            DependencyProperty.Register(nameof(IsFavorited), typeof(bool), typeof(FavoritesToggle), new PropertyMetadata(false));

        public bool IsFavorited
        {
            get => (bool)GetValue(IsFavoritedProperty);
            set => SetValue(IsFavoritedProperty, value);
        }

        public FavoritesToggle()
        {
            InitializeComponent();
        }
    }
}
