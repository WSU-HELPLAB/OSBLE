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
using System.Windows.Browser;
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
            InstallButton.Click += new RoutedEventHandler(InstallButton_Click);

            if (Application.Current.IsRunningOutOfBrowser)
            {
                InstallButton.Visibility = System.Windows.Visibility.Collapsed;
                login.Show();
            }

        }

        void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.InstallState == InstallState.NotInstalled)
            {
                Application.Current.Install();
            }
            else
            {
                HtmlPage.Window.Alert("The OSBLE File Uploader must be launched separately from your desktop.");
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
    }
}
