using System.Windows.Controls;
using ReviewInterfaceBase.ViewModel.DocumentHolder;

namespace ReviewInterfaceBase.View.DocumentHolder
{
    public partial class SpatialDocumentHolderView : UserControl, IDocumentHolderView
    {
        private IDocumentHolderViewModel thisVM;

        public SpatialDocumentHolderView(IDocumentHolderViewModel viewModel)
        {
            thisVM = viewModel;

            InitializeComponent();
        }

        public IDocumentHolderViewModel GetViewModel()
        {
            return thisVM;
        }

    }
}