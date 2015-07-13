using OSBIDE.Controls.ViewModels;
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

namespace OSBIDE.Controls.Views
{
    /// <summary>
    /// Interaction logic for AskForHelpForm.xaml
    /// </summary>
    public partial class AskForHelpForm : Window
    {
        public AskForHelpForm()
        {
            InitializeComponent();
            LoadingIcon.Image = OSBIDE.Controls.Properties.Resources.ajax_loader;
            LoadingIcon.VisibleChanged += LoadingIcon_VisibleChanged;
            LoadingIcon.Visible = false;
            SubmitButton.IsEnabled = false;
            UserComment.KeyDown += UserComment_KeyDown;
        }

        void UserComment_KeyDown(object sender, KeyEventArgs e)
        {
            if (UserComment.Text.Trim().Length > 0)
            {
                SubmitButton.IsEnabled = true;
            }
            else
            {
                SubmitButton.IsEnabled = false;
            }
        }

        public AskForHelpViewModel ViewModel
        {
            get
            {
                return this.DataContext as AskForHelpViewModel;
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

        void LoadingIcon_VisibleChanged(object sender, EventArgs e)
        {
            //a little heavy-handed, but the loading icon keeps getting set to "on" when it should be off.
            //This should correct any problem.
            if (ViewModel != null)
            {
                LoadingIcon.Visible = ViewModel.LoadingIconVisible;
            }
        }

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LoadingIconVisible")
            {
                LoadingIcon.Visible = ViewModel.LoadingIconVisible;
            }
        }

        void ViewModel_RequestClose(object sender, EventArgs e)
        {
            this.Close();
        }

        public static MessageBoxResult ShowModalDialog(AskForHelpViewModel vm)
        {
            AskForHelpForm window = new AskForHelpForm();
            window.ViewModel = vm;
            window.ShowDialog();
            return vm.Result;
        }

    }
}
