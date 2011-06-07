using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace TeamCreation
{
    public partial class MainPage : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion INotifyPropertyChanged Members

        public MainPage()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void ClearTeams_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
        }

        private void combos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void PublishChanges_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}