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
using System.Windows.Threading;

namespace FileUploader.Controls
{
    public partial class LoginWindow : ChildWindow
    {
        private UploaderWebServiceClient client = new UploaderWebServiceClient();
        public EventHandler ValidTokenReceived = delegate { };
        private DispatcherTimer timer;
        public string Token
        {
            get;
            set;
        }
        public LoginWindow()
        {
            InitializeComponent();
            client.ValidateUserCompleted += new EventHandler<ValidateUserCompletedEventArgs>(client_ValidateUserCompleted);
            PasswordBox.KeyUp += new KeyEventHandler(PasswordBox_KeyUp);

            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 10);
            timer.Tick += new EventHandler(timer_Tick);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            OKButton.IsEnabled = true;
            MessageBox.Show("There was an error validating your credentials.  Please try again.  If the problem persists, please contact OSBLE support.");
            timer.Stop();
        }

        void PasswordBox_KeyUp(object sender, KeyEventArgs e)
        {
            //shortcut for logon
            if (e.Key == Key.Enter)
            {
                OKButton_Click(this, new RoutedEventArgs());
            }
        }

        void client_ValidateUserCompleted(object sender, ValidateUserCompletedEventArgs e)
        {
            Token = e.Result;
            timer.Stop();
            OKButton.IsEnabled = true;

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
            OKButton.IsEnabled = false;
            timer.Start();
            client.ValidateUserAsync(UserNameTextBox.Text, this.PasswordBox.Password);
        }

    }
}

