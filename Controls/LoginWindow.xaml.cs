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
using FileUploader.OsbleServices;

namespace FileUploader.Controls
{
    public partial class LoginWindow : ChildWindow
    {
        private UploaderWebServiceClient client = new UploaderWebServiceClient();
        public EventHandler ValidTokenReceived = delegate { };
        public string Token
        {
            get;
            set;
        }
        public LoginWindow()
        {
            InitializeComponent();
            client.ValidateUserCompleted += new EventHandler<ValidateUserCompletedEventArgs>(client_ValidateUserCompleted);
        }

        void client_ValidateUserCompleted(object sender, ValidateUserCompletedEventArgs e)
        {
            Token = e.Result;

            //if we received a valid token, tell the caller that we're good to continue
            if (Token.Length > 0)
            {
                ErrorTextBlock.Visibility = System.Windows.Visibility.Collapsed;
                ValidTokenReceived(this, EventArgs.Empty);
                this.DialogResult = true;
            }
            else
            {
                ErrorTextBlock.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            client.ValidateUserAsync(UserNameTextBox.Text, this.PasswordBox.Password);
        }

    }
}

