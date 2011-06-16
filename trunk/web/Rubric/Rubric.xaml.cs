using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;

namespace OsbleRubric
{
    public partial class Rubric : UserControl
    {
        public Rubric()
        {
            InitializeComponent();
        }

        private void PublishChanges_Click(object sender, RoutedEventArgs e)
        {
            //umm we should actually save the changes but until then tell the user they were saved :)
            MessageBox.Show("Changes Saved");
        }

        private void CancelChanges_Click(object sender, RoutedEventArgs e)
        {
            HtmlPage.Window.Invoke("CloseRubric", "");
        }
    }
}