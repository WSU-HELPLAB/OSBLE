using System.Windows.Controls;
using ReviewInterfaceBase.ViewModel.DocumentHolder;

namespace ReviewInterfaceBase.View.DocumentHolder
{
    public partial class VideoDocumentHolderView : UserControl, IDocumentHolderView
    {
        private IDocumentHolderViewModel thisVM;

        public VideoDocumentHolderView(IDocumentHolderViewModel viewModel)
        {
            InitializeComponent();

            thisVM = viewModel;
        }

        public IDocumentHolderViewModel GetViewModel()
        {
            return thisVM;
        }
    }
}