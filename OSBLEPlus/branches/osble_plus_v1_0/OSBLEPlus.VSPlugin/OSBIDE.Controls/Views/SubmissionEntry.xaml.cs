using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OSBIDE.Controls.ViewModels;

namespace OSBIDE.Controls.Views
{
    /// <summary>
    /// Interaction logic for SubmissionActivity.xaml
    /// </summary>
    public partial class SubmissionEntry : UserControl
    {
        public SubmissionEntryViewModel ViewModel
        {
            get
            {
                return this.DataContext as SubmissionEntryViewModel;
            }
            set
            {
                this.DataContext = value;
            }
        }

        public SubmissionEntry()
        {
            InitializeComponent();
        }
    }
}
