namespace OSBIDE.Controls.ViewModels
{
    public class BrowserViewModel : ViewModelBase
    {
        private string _url = string.Empty;
        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
                OnPropertyChanged("Url");
            }
        }

        private string _authKey = string.Empty;
        public string AuthKey
        {
            get
            {
                return _authKey;
            }
            set
            {
                _authKey = value;
                OnPropertyChanged("AuthKey");
            }
        }
    }
}
