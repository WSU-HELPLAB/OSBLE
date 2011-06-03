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
        UploaderWebServiceClient client = new UploaderWebServiceClient();

        public LoginWindow()
        {
            InitializeComponent();
            client.ValidateUserCompleted += new EventHandler<ValidateUserCompletedEventArgs>(client_ValidateUserCompleted);
        }

        void client_ValidateUserCompleted(object sender, ValidateUserCompletedEventArgs e)
        {
            
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            client.ValidateUserAsync(UserNameTextBox.Text, this.PasswordBox.Password);
        }

    }
}

