using ReviewInterfaceBase.Model.FindWindow;

namespace ReviewInterfaceBase.ViewModel.FindWindow
{
    public class FindWindowOptions
    {
        private FindWindowModel findWindowModel;

        public bool MatchCase
        {
            get { return findWindowModel.MatchCase; }
        }

        public bool MatchWholeWord
        {
            get { return findWindowModel.MatchWholeWord; }
        }

        public SearchIn SearchIn
        {
            get { return findWindowModel.SearchIn; }
        }

        public string LookingFor
        {
            get
            {
                return findWindowModel.LookingFor;
            }
        }

        public FindWindowOptions(FindWindowModel findWindowModel)
        {
            this.findWindowModel = findWindowModel;
        }
    }
}