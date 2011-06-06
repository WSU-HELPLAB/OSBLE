using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Media.Imaging;

using FileUploader.OsbleServices;
using FileUploader.Controls;
using System.Windows.Navigation;
namespace FileUploader
{
    public partial class MainPage : UserControl
    {
        private LoginWindow login = new LoginWindow();
        public MainPage()
        {
            InitializeComponent();
            
            //event handlers
            login.ValidTokenReceived += new EventHandler(ValidTokenReceived);


            if (Application.Current.InstallState == InstallState.NotInstalled)
            {
                InstallBtn.Visibility = Visibility.Visible;
            }
            else
            {
                InstallBtn.Visibility = Visibility.Collapsed;

                // apparently, you can't make a button to launch the already installed application
                InstallText.Text = "This application is already installed on your computer.";
            }

            if (Application.Current.IsRunningOutOfBrowser)
            {
                login.Show();
            }

        }


        /// <summary>
        /// Called once we receive a valid login token
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValidTokenReceived(object sender, EventArgs e)
        {
            this.Content = new UploaderPage(login.Token);
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.InstallState == InstallState.NotInstalled)
            {
                Application.Current.Install();
            }
        }
    }
}
