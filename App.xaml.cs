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
using System.Reflection;

namespace FileUploader
{
    public partial class App : Application
    {
        public String VersionNumber
        {
            get;
            set;
        }
        public App()
        {
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;
            this.CheckAndDownloadUpdateCompleted += new CheckAndDownloadUpdateCompletedEventHandler(App_CheckAndDownloadUpdateCompleted);
            InitializeComponent();
        }

        void App_CheckAndDownloadUpdateCompleted(object sender, CheckAndDownloadUpdateCompletedEventArgs e)
        {
            if (e.UpdateAvailable)
            {
                MessageBox.Show("An update for the OSBLE File Uploader is available.  To install, please restart the application");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("An error occurred while attempting to check for a newer version of the OSBLE File Uploader.");
            }
        }

        public static void Navigate(UserControl newPage)
        {
            App currentApp = (App)Application.Current;
            currentApp.rootGrid.Children.Clear();
            currentApp.rootGrid.Children.Add(newPage);
        }

        private Grid rootGrid = new Grid();
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.CheckAndDownloadUpdateAsync();
            this.RootVisual = rootGrid;
            rootGrid.Children.Add(new MainPage());

            //find our version number
            Assembly asm = Assembly.GetExecutingAssembly();
            if (asm.FullName != null)
            {
                AssemblyName assemblyName = new AssemblyName(asm.FullName);
                VersionNumber = assemblyName.Version.ToString();
            }
        }

        private void Application_Exit(object sender, EventArgs e)
        {

        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
            else
            {
                e.Handled = true;
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");
                MessageBox.Show(errorMsg);
            }
        }

        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }
    }
}
