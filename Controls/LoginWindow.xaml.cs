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
using System.IO.IsolatedStorage;

namespace FileUploader.Controls
{
    public partial class LoginWindow : ChildWindow
    {
        //I dislike using strings as keys.  Enums are nicer
        private enum IsolatedStorageKeys { SaveCredentials, UserName, Password };

        //reference to our web service
        private UploaderWebServiceClient client = new UploaderWebServiceClient();

        /// <summary>
        /// Event handler that signals when the control received a valid login token
        /// </summary>
        public EventHandler ValidTokenReceived = delegate { };

        //connection timeout timer
        private DispatcherTimer timer;

        /// <summary>
        /// Gets or sets the authentication token sent by the server
        /// </summary>
        public string Token
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor method.  Nuff said
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();

            //retrieve user / pass if it was previously saved
            if (
                IsolatedStorageSettings.ApplicationSettings.Contains(IsolatedStorageKeys.UserName.ToString())
                &&
                IsolatedStorageSettings.ApplicationSettings.Contains(IsolatedStorageKeys.Password.ToString())
                &&
                IsolatedStorageSettings.ApplicationSettings.Contains(IsolatedStorageKeys.SaveCredentials.ToString())
                &&
                (bool)IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.SaveCredentials.ToString()]
                )
            {
                UserNameTextBox.Text = IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.UserName.ToString()].ToString();
                this.PasswordBox.Password = IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.Password.ToString()].ToString();
                this.RememberCredentialsCheckBox.IsChecked = true;
            }

            //UI event listeners
            client.ValidateUserCompleted += new EventHandler<ValidateUserCompletedEventArgs>(client_ValidateUserCompleted);
            PasswordBox.KeyUp += new KeyEventHandler(TextBox_KeyUp);
            UserNameTextBox.KeyUp += new KeyEventHandler(TextBox_KeyUp);
            RememberCredentialsCheckBox.Checked += new RoutedEventHandler(RememberCredentialsCheckBox_Checked);
            this.KeyDown += new KeyEventHandler(LoginWindow_KeyDown);

            //set up our "connection timeout" timer.  Currently set to to a 10 second timeout.  May
            //need to adjust.
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 10);
            timer.Tick += new EventHandler(timer_Tick);
        }

        void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            //CTRL + V = display version number
            if (e.Key == Key.V)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    MessageBox.Show("File Uploader version " + (App.Current as FileUploader.App).VersionNumber);
                }
            }
        }

        /// <summary>
        /// Toggles the remembering of user credentials
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RememberCredentialsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //code is more generic if we don't reference the checkbox by name
            CheckBox cb = sender as CheckBox;

            if ((bool)cb.IsChecked)
            {
                //let the application know that it's okay to save user & pass
                IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.SaveCredentials.ToString()] = true;
            }
            else
            {
                //let the application know that it's okay to save user & pass
                IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.SaveCredentials.ToString()] = false;
            }
        }

        /// <summary>
        /// Handles connection timeout issues
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            OKButton.IsEnabled = true;
            MessageBox.Show("There was an error validating your credentials.  Please try again.  If the problem persists, please contact OSBLE support.");
            timer.Stop();
        }

        /// <summary>
        /// Listens for keyup presses on our user name and password textbox.  Allows "enter" to trigger
        /// authentication.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //shortcut for logon
            if (e.Key == Key.Enter)
            {
                OKButton_Click(this, new RoutedEventArgs());
            }
        }

        /// <summary>
        /// Called when we receive a response from the validation server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void client_ValidateUserCompleted(object sender, ValidateUserCompletedEventArgs e)
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

        /// <summary>
        /// Handles the validation call to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            OKButton.IsEnabled = false;

            //save credentials if desired
            if ((bool)RememberCredentialsCheckBox.IsChecked)
            {
                IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.UserName.ToString()] = UserNameTextBox.Text;
                IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.Password.ToString()] = this.PasswordBox.Password;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.UserName.ToString()] = "";
                IsolatedStorageSettings.ApplicationSettings[IsolatedStorageKeys.Password.ToString()] = "";
            }

            timer.Start();
            client.ValidateUserAsync(UserNameTextBox.Text, this.PasswordBox.Password);
        }

    }
}

