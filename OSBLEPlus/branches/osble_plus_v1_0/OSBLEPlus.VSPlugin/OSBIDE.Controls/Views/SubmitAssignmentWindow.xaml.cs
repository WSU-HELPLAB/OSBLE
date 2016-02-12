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
using System.Windows.Shapes;
using OSBIDE.Controls.ViewModels;

namespace OSBIDE.Controls.Views
{
    /// <summary>
    /// Interaction logic for SubmitAssignmentWindow.xaml
    /// </summary>
    public partial class SubmitAssignmentWindow : Window
    {
        public SubmitAssignmentViewModel ViewModel
        {
            get
            {
                return this.DataContext as SubmitAssignmentViewModel;
            }
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.RequestClose -= new EventHandler(ViewModel_RequestClose);
                    ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
                this.DataContext = value;
                ViewModel.RequestClose += new EventHandler(ViewModel_RequestClose);
                ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //update loading icon status
            LoadingIcon.Visible = ViewModel.IsLoading;
        }

        void ViewModel_RequestClose(object sender, EventArgs e)
        {
            this.Close();
        }

        public SubmitAssignmentWindow()
        {
            InitializeComponent();

            LoadingIcon.Image = OSBIDE.Controls.Properties.Resources.ajax_loader;
            LoadingIcon.VisibleChanged += LoadingIcon_VisibleChanged;
            LoadingIcon.Visible = false;
        }

        void LoadingIcon_VisibleChanged(object sender, EventArgs e)
        {
            //a little heavy-handed, but the loading icon keeps getting set to "on" when it should be off.
            //This should correct any problem.
            if (ViewModel != null)
            {
                LoadingIcon.Visible = ViewModel.IsLoading;
            }
        }

        public static MessageBoxResult ShowModalDialog(SubmitAssignmentViewModel vm)
        {
            SubmitAssignmentWindow window = new SubmitAssignmentWindow();
            window.ViewModel = vm;
            window.ShowDialog();
            return vm.Result;
        }
    }
}
