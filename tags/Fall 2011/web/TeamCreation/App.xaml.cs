using System;
using System.Windows;
using System.Windows.Browser;

namespace TeamCreation
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
            // Get JSON string of activities or pass empty list if none passed in.
            string SerializedTeamMembersJSON = "";

            if (e.InitParams.Keys.Contains("teamMembers"))
            {
                SerializedTeamMembersJSON = Uri.UnescapeDataString(e.InitParams["teamMembers"]);
            }

            ////////Test data////////
            //SerializedTeamMembersJSON = Uri.UnescapeDataString("%5B%7B%22isUser%22%3Atrue%2C%22UserID%22%3A4%2C%22TeamID%22%3A0%2C%22Name%22%3A%22John%20Morgan%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A5%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Margaret%20Bailey%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A6%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Carol%20Jackson%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A7%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Donald%20Robinson%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A8%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Paul%20Sanders%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A9%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Anthony%20Stewart%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A10%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Paul%20Harris%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A11%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Donald%20White%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A12%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Christopher%20Sanders%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A13%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Robert%20Wright%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A14%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Betty%20Rogers%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A15%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Nancy%20Russell%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%2C%7B%22isUser%22%3Atrue%2C%22UserID%22%3A16%2C%22TeamID%22%3A0%2C%22Name%22%3A%22Jason%20Robinson%22%2C%22Section%22%3A0%2C%22IsModerator%22%3Afalse%2C%22Subbmitted%22%3Afalse%2C%22InTeamID%22%3A0%2C%22InTeamName%22%3Anull%7D%5D");
            ////////////////////////

            MainPage mp = new MainPage(SerializedTeamMembersJSON);
            this.RootVisual = mp;

            HtmlPage.RegisterScriptableObject("MainPage", mp);
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