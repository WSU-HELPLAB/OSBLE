﻿using System;
using System.Windows;

using ReviewInterfaceBase.ViewModel;

namespace ReviewInterfaceBase
{
    public partial class App : Application
    {
        public App()
        {
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.InitParams != null)
            {
                foreach (var data in e.InitParams)
                {
                    //GET INIT parameters here.  This is probably going to be some sort of session key.
                    //So my SL application can tell the web services what it is and what it needs
                    if (data.Key == "SessionKey")
                    {
                        //this needs to tell the web service what our sessionKey is
                        (new ReviewInterfaceBase.Web.FakeDomainContext()).SetSessionID(data.Key);
                    }
                }
            }
            MainPageViewModel viewModel = new MainPageViewModel();
            MainPageView mpv = viewModel.GetView();

            EventHandler handler = null;
            handler = delegate
            {
                viewModel.RequestClose -= handler;
                this.Application_Exit(sender, e);
            };
            viewModel.RequestClose += handler;

            this.RootVisual = mpv;
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
                MessageBox.Show("Message:" + e.ExceptionObject.Message + "\r" + "InnerExcetion:" + e.ExceptionObject.InnerException + "\r" + "StackTrace:" + e.ExceptionObject.StackTrace);
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