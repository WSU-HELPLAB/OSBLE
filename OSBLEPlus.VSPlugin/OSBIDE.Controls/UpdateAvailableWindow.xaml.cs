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
using System.Diagnostics;

namespace OSBIDE.Controls
{
    /// <summary>
    /// Interaction logic for UpdateAvailableWindow.xaml
    /// </summary>
    public partial class UpdateAvailableWindow : Window
    {
        public MessageBoxResult Result { get; private set; }

        public UpdateAvailableWindow()
        {
            InitializeComponent();
            UpdateLinkText.DataContext = UpdateLink;
            Result = MessageBoxResult.None;

            OkButton.Click += new RoutedEventHandler(OkButton_Click);
            UpdateLink.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(UpdateLink_RequestNavigate);
            
        }

        void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            this.Close();
        }

        void UpdateLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public static MessageBoxResult ShowModalDialog(string updateUrl)
        {
            UpdateAvailableWindow window = new UpdateAvailableWindow();
            window.UpdateLink.NavigateUri = new Uri(updateUrl);
            window.ShowDialog();
            return window.Result;
        }
    }
}
