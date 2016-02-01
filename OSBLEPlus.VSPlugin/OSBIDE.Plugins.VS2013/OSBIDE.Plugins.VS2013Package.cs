using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Caching;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using stdole;

using OSBIDE.Controls;
using OSBIDE.Library.ServiceClient;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBIDE.Plugins.Base;
using OSBLEPlus.Logic.Utility.Logging;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(ActivityFeedToolWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [ProvideToolWindow(typeof(ActivityFeedDetailsToolWindow), Style = VsDockStyle.MDI, MultiInstances = true)]
    [ProvideToolWindow(typeof(ChatToolWindow), Style = VsDockStyle.MDI)]
    [ProvideToolWindow(typeof(UserProfileToolWindow), Style = VsDockStyle.MDI)]
    [ProvideToolWindow(typeof(CreateAccountToolWindow), Style = VsDockStyle.MDI)]
    [ProvideToolWindow(typeof(GenericOsbideToolWindow), Style = VsDockStyle.MDI)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    [Guid(CommonGuidList.guidOSBIDE_VSPackagePkgString)]
    public sealed partial class OsbidePluginsVs2013Package : Package, IDisposable, IVsShellPropertyEvents
    {
        private readonly ILogger _errorLogger = new LocalErrorLogger();
        private readonly FileCache _cache = Cache.CacheInstance;
        private readonly RijndaelManaged _encoder = new RijndaelManaged();

        private VsEventHandler _eventHandler;
        private ServiceClient _client;
        private OsbideToolWindowManager _manager;
        private CommandBarEvents _osbideErrorListEvent;

        private string _userName;
        private string _userPassword;

        private uint _eventSinkCookie;
        private bool _hasStartupErrors;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public OsbidePluginsVs2013Package()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", ToString()));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            base.Initialize();

            LoadAssemblies();

            InitializeMenuCommand();
            InitOsbideErrorContextMenu();

            InitAndEncryptLocalLogin();

            SetupServiceClient();
            UpdateServiceConnectionStatus();

            //set up tool window manager
            _manager = new OsbideToolWindowManager(_cache, this);

            //display a user notification if we don't have any user on file
            if (_userName == null || _userPassword == null)
            {
                _hasStartupErrors = true;
                MessageBox.Show("Thank you for installing the OSBLE+ Visual Studio Plugin.  To complete the installation, you must enter your user information, which can be done by clicking on the \"Tools\" menu and selecting \"Log into OSBLE+\".", "Welcome to OSBLE+", MessageBoxButton.OK);
            }
            else 
            {
                try
                {
                    Login(_userName, _userPassword);
                }
                catch (Exception ex)
                {
                    _errorLogger.WriteToLog("Web service error: " + ex.Message, LogPriority.HighPriority);
                    _hasStartupErrors = true;
                }
            }
        }

        private void ToggleProfileImage(bool hasSocial)
        {
            var dte = (DTE2)GetService(typeof(SDTE));
            var cbs = ((CommandBars)dte.CommandBars);
            var cb = cbs["OSBLE+"];
            var toolsControl = cb.Controls["My OSBLE+ Profile"];
            var profileButton = (CommandBarButton)toolsControl;

            if (hasSocial)
            {
                profileButton.Picture = (StdPicture)IconConverter.GetIPictureDispFromImage(Resources.profile_new_social);
                profileButton.TooltipText = "New social activity detected";
            }
            else
            {
                profileButton.Picture = (StdPicture)IconConverter.GetIPictureDispFromImage(Resources.profile);
                profileButton.TooltipText = "View your profile";
            }
        }

        private void UpdateServiceConnectionStatus()
        {
            var dte = (DTE2)GetService(typeof(SDTE));
            var cbs = ((CommandBars)dte.CommandBars);
            var cb = cbs["OSBLE+"];
            var toolsControl = cb.Controls["Log into OSBLE+"];
            var loginButton = (CommandBarButton)toolsControl;

            if (_client.IsSendingData)
            {
                loginButton.Picture = (StdPicture)IconConverter.GetIPictureDispFromImage(Resources.login_active);
                loginButton.TooltipText = "Connected to OSBLE+";
            }
            else
            {
                loginButton.Picture = (StdPicture)IconConverter.GetIPictureDispFromImage(Resources.login);
                loginButton.TooltipText = "Not connected to OSBLE+. Click to log in.";
            }
        }

        public int OnShellPropertyChange(int propid, object propValue)
        {
            // --- We handle the event if zombie state changes from true to false
            if ((int)__VSSPROPID.VSSPROPID_Zombie == propid)
            {
                if ((bool)propValue == false)
                {
                    // --- Show the commandbar
                    var dte = GetService(typeof(SDTE)) as DTE2;
                    var cbs = ((CommandBars)dte.CommandBars);
                    var cb = cbs["OSBLE+"];
                    cb.Visible = true;

                    // --- Unsubscribe from events
                    var shellService = GetService(typeof(SVsShell)) as IVsShell;
                    if (shellService != null)
                    {
                        ErrorHandler.ThrowOnFailure(shellService.
                          UnadviseShellPropertyChanges(_eventSinkCookie));
                    }
                    _eventSinkCookie = 0;
                }
            }
            return VSConstants.S_OK;
        }

        private void ShowAwesomiumError(Exception ex)
        {
            if (ex != null)
            {
                _errorLogger.WriteToLog("Awesomium Error: " + ex.Message, LogPriority.HighPriority);
            }
            MessageBox.Show("It appears as though your system is missing prerequisite components necessary for the OSBLE+ Visual Studio Plugin to operate properly.  Until this is resolved, you will not be able to access certain OSBLE+ components within Visual Studio.  You can download the prerequisite files and obtain support by visiting http://osble.codeplex.com.", "OSBLE+", MessageBoxButton.OK);
        }
    }
}
