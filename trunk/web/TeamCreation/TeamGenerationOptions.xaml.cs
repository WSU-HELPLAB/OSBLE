using System.Windows;
using System.Windows.Controls;

namespace TeamCreation
{
    public partial class TeamGenerationOptions : ChildWindow
    {
        public TeamGenerationOptions()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void DoGenerate(object sender, RoutedEventArgs e)
        {
        }

        private void AssignmentMode_Change(object sender, RoutedEventArgs e)
        {
        }

        private void TeamsUpDown_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void StudentsUpDown_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void TeamsUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
        }

        private void StudentsUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }
    }
}