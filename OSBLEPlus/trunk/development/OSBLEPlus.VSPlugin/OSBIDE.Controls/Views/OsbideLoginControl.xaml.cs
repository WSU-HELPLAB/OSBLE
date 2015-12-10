using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using OSBIDE.Controls.ViewModels;

namespace OSBIDE.Controls.Views
{
    /// <summary>
    /// Interaction logic for OsbideLoginControl.xaml
    /// </summary>
    public partial class OsbideLoginControl
    {
        public OsbideLoginControl()
        {
            InitializeComponent();
            LoadingIcon.Image = Properties.Resources.ajax_loader;
            LoadingIcon.VisibleChanged += LoadingIcon_VisibleChanged;
            LoadingIcon.Visible = false;
            PasswordTextBox.KeyDown += PasswordTextBox_KeyDown;
            PasswordTextBox.PasswordChanged += PasswordTextBox_PasswordChanged;

            CreateAccountLink.RequestNavigate += RequestNavigate;
            PrivacyPolicyLink.RequestNavigate += RequestNavigate;
            ForgotEmailLink.RequestNavigate += RequestNavigate;
            ForgotPasswordLink.RequestNavigate += RequestNavigate;
        }

        void PasswordTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var passwordBox = e.OriginalSource as PasswordBox;
                if (passwordBox != null)
                    ViewModel.Password = passwordBox.Password;
            }
        }

        void RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
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

        void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (ViewModel != null)
                {
                    ViewModel.LoginCommand.Execute(this);
                    e.Handled = true;
                }
            }
        }

        public OsbleLoginViewModel ViewModel
        {
            get
            {
                return DataContext as OsbleLoginViewModel;
            }
            set
            {
                if (ViewModel != null)
                {
                    ViewModel.RequestClose -= ViewModel_RequestClose;
                    ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
                DataContext = value;
                if (ViewModel != null)
                {
                    ViewModel.RequestClose += ViewModel_RequestClose;
                    ViewModel.PropertyChanged += ViewModel_PropertyChanged;

                    //we have to bind passwords manually
                    PasswordTextBox.Password = ViewModel.Password;
                }
            }
        }

        void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LoadingIconVisible")
            {
                LoadingIcon.Visible = ViewModel.LoadingIconVisible;
            }
            else if (e.PropertyName == "Password")
            {
                if (PasswordTextBox.Password != ViewModel.Password)
                {
                    PasswordTextBox.Password = ViewModel.Password;
                }
            }
            else if (e.PropertyName == "Logout")
            {
                // remove the text from the boxes when user logs out
                EmailTextBox.Text = String.Empty;
                PasswordTextBox.Password = String.Empty;                

                // refresh awesomium without cache to ensure that the cache key is not used for the old user
            }
        }

        void ViewModel_RequestClose(object sender, EventArgs e)
        {
            Close();
        }

        public static MessageBoxResult ShowModalDialog(OsbleLoginViewModel vm)
        {
            OsbideLoginControl window = new OsbideLoginControl();
            window.ViewModel = vm;
            window.ShowDialog();
            return vm.Result;
        }
    }
}
