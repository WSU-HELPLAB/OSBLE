using System;
using System.Windows;
using System.Windows.Input;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Controls.ViewModels
{
    public class OsbleLoginViewModel : ViewModelBase
    {
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _authenticationHash = string.Empty;
        private string _errorText = string.Empty;

        private bool _loadingIconVisible;
        private bool _buttonsEnabled;
        private bool _isLoggedIn;

        public event EventHandler RequestClose = delegate { };
        public event EventHandler RequestCreateAccount = delegate { };        

        public OsbleLoginViewModel()
        {
            LoginCommand = new DelegateCommand(Login, CanIssueCommand);
            LogoutCommand = new DelegateCommand(Logout, CanIssueCommand);
            CancelCommand = new DelegateCommand(Cancel, CanIssueCommand);
            CreateAccountCommand = new DelegateCommand(CreateAccount, CanIssueCommand);            

            _buttonsEnabled = true;
            _loadingIconVisible = false;
        }

        #region properties
        public ICommand LoginCommand { get; set; }
        public ICommand LogoutCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand CreateAccountCommand { get; set; }        
        public MessageBoxResult Result { get; private set; }

        public bool IsLoggedIn
        {
            get
            {
                return _isLoggedIn;
            }
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged("IsLoggedIn");
            }
        }

        public string ErrorText
        {
            get
            {
                return _errorText;
            }
            set
            {
                _errorText = value;
                OnPropertyChanged("ErrorText");
            }
        }

        public string AuthenticationHash
        {
            get
            {
                return _authenticationHash;
            }
            set
            {
                _authenticationHash = value;
                OnPropertyChanged("AuthenticationHash");
            }
        }

        public bool ButtonsEnabled
        {
            get
            {
                return _buttonsEnabled;
            }
            set
            {
                _buttonsEnabled = value;
                OnPropertyChanged("ButtonsEnabled");
            }
        }

        public bool LoadingIconVisible
        {
            get
            {
                return _loadingIconVisible;
            }
            set
            {
                _loadingIconVisible = value;
                OnPropertyChanged("LoadingIconVisible");
            }
        }

        public string Email
        {
            get
            {
                return _email;
            }
            set
            {
                _email = value;
                OnPropertyChanged("Email");
            }
        }

        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }
        #endregion

        private async void Logout(object param)
        {
            try
            {
                var task = AsyncServiceClient.Logout();
                var result = await task;

                Result = MessageBoxResult.No;
                RequestClose(this, EventArgs.Empty);

                // need to clear out the logged in username/pw
                // refresh awesomium
                OnPropertyChanged("Logout");
            }
            catch (Exception e)
            {
                Result = MessageBoxResult.No;
                RequestClose(this, EventArgs.Empty);

                // need to clear out the logged in username/pw
                // refresh awesomium
                OnPropertyChanged("Logout");
                MessageBox.Show("There was an error logging you out from OSBLE+. If this issue persists, please contact support@osble.org with the following error message.\n\nError: " + e.InnerException.ToString(), "Log Into OSBLE+", MessageBoxButton.OK);                
            }

           
            
        }

        private async void Login(object param)
        {
            //begin login attempt
            LoadingIconVisible = true;
            ButtonsEnabled = false;
            ErrorText = string.Empty;

            //TODO: handle the try catch exception
            try
            {
                var task = AsyncServiceClient.Login(Email, Password);
                var result = await task;

                LoginCompleted(result);
            }
            catch (Exception e)
            {
                ErrorText = "Unable to connect to OSBLE+";                
                LoginCompleted("exception");
            }
            
        }

        private void LoginCompleted(string result)
        {
            LoadingIconVisible = false;
            ButtonsEnabled = true;

            if(!result.Equals("exception"))
            {
                //hash longer than 0 means success
                try
                {
                    if (result.Length > 0)
                    {
                        AuthenticationHash = result;
                        Result = MessageBoxResult.OK;
                        RequestClose(this, EventArgs.Empty);
                    }
                    else
                    {
                        ErrorText = "Invalid email or password.";
                    }
                }
                catch (Exception)
                {
                    ErrorText = "Error processing request (http).";
                }
            }                        
        }

        private void CreateAccount(object param)
        {
            RequestCreateAccount(this, EventArgs.Empty);
            Cancel(this);
        }
        
        private void Cancel(object param)
        {
            Result = MessageBoxResult.Cancel;
            RequestClose(this, EventArgs.Empty);
        }

        private bool CanIssueCommand(object param)
        {
            return true;
        }

        public string CreateAccountUrl
        {
            get
            {
                return string.Format("{0}/Account/Create", StringConstants.WebClientRoot);
            }
        }
    }
}
