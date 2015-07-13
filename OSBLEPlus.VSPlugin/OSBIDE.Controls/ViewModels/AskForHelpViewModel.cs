using System;
using System.Windows;
using System.Windows.Input;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBLE.Interfaces;

namespace OSBIDE.Controls.ViewModels
{
    public class AskForHelpViewModel : ViewModelBase
    {
        public ICommand SubmitCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public event EventHandler RequestClose = delegate { };


        public IUser CurrentUser { get; set; }
        public MessageBoxResult Result { get; private set; }

        private bool _loadingIconVisible;
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

        private bool _buttonsEnabled = true;
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

        private string _code = "";
        public string Code
        {
            get
            {
                return _code;
            }
            set
            {
                _code = value;
                OnPropertyChanged("Code");
            }
        }

        private string _userText = "";
        public string UserText
        {
            get
            {
                return _userText;
            }
            set
            {
                _userText = value;
                OnPropertyChanged("UserText");
            }
        }

        public AskForHelpViewModel()
        {
            SubmitCommand = new DelegateCommand(Submit, CanIssueCommand);
            CancelCommand = new DelegateCommand(Cancel, CanIssueCommand);
        }

        private void Submit(object param)
        {
            Result = MessageBoxResult.OK;
            RequestClose(this, EventArgs.Empty);
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
    }
}
