using System.ComponentModel;
using ReviewInterfaceBase.Model.Tag;
using ReviewInterfaceBase.View.Tag;

namespace ReviewInterfaceBase.ViewModel.Tag
{
    public class TagViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private TagView thisView;
        private TagModel thisModel;

        public string Text
        {
            get { return thisModel.Text; }
        }

        public TagViewModel(string name)
        {
            thisModel = new TagModel(name);
            thisView = new TagView();

            //this needs to happen for bindings to work
            thisView.DataContext = this;
        }

        public TagView GetView()
        {
            return thisView;
        }
    }
}