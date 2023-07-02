using Prism.Commands;
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
    /// Interaction logic for UpdateAvailable.xaml
    /// </summary>
    public partial class UpdateAvailable : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public void NPC(string propertyName) => NotifyPropertyChanged(propertyName);

        public Octokit.Release Release { get; }

        public UpdateAvailable(Octokit.Release LatestRelease)
        {
            InitializeComponent();
            Release = LatestRelease;
            DataContext = this;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        public DelegateCommand<object> OpenInBrowser => new(_ => GeneralUtils.OpenUrl(Release.HtmlUrl, false));
    }
}
