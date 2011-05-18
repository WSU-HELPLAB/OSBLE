using System.ComponentModel;

using ReviewInterfaceBase.ViewModel.FindWindow;

namespace ReviewInterfaceBase.Model.FindWindow
{
    public class FindWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private bool matchCase;
        private bool matchWholeWord;
        private bool searchUp;
        private SearchIn searchIn;
        private string lookingFor;

        public bool MatchCase
        {
            get { return matchCase; }
            set
            { matchCase = value; NotifyPropertyChanged("MatchCase"); }
        }

        public bool MatchWholeWord
        {
            get { return matchWholeWord; }
            set { matchWholeWord = value; NotifyPropertyChanged("MatchWholeWord"); }
        }

        public bool SearchUp
        {
            get { return searchUp; }
            set { searchUp = value; NotifyPropertyChanged("SearchUp"); }
        }

        public SearchIn SearchIn
        {
            get { return searchIn; }
            set { searchIn = value; NotifyPropertyChanged("SearchIn"); }
        }

        public string LookingFor
        {
            get { return lookingFor; }
            set { lookingFor = value; NotifyPropertyChanged("LookingFor"); }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}