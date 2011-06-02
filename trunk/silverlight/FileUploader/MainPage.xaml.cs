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
        public MainPage()
        {
            InitializeComponent();

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
                // go to UploaderPage
                this.Content = new FileUploader();
            }

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
