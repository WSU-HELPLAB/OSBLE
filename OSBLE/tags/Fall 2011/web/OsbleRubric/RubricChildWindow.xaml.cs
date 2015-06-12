using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OsbleRubric
{
    public partial class RubricChildWindow : ChildWindow
    {
        public RubricChildWindow(List<string> errorList)
        {
            InitializeComponent();
            setUpTextBox(errorList);
        }

        private void setUpTextBox(List<string> errorList)
        {
            if(errorList.Count == 1)
            {
                this.TextBlock.Text = "An error has occured while attempting to save:\n\n ";
            }
            else if (errorList.Count == 0) //Should be a useless check, shouldn't create error child window if there are no errors
            {
                this.TextBlock.Text = "No errors!";
            }
            else
            {
                this.TextBlock.Text = "Errors have occured while attempting to save:\n\n";
            }

            //Adding error(s) to the textblock to display
            foreach (string s in errorList)
            {
                this.TextBlock.Text += " - " + s + "\n";
            }

            this.TextBlock.Text += "\nPlease fix all errors before attempting to save again.";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

