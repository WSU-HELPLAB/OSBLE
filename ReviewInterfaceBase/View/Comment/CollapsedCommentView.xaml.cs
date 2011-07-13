using System.Windows.Controls;

using ReviewInterfaceBase.ViewModel.Comment;

namespace ReviewInterfaceBase.View.Comment
{
    public partial class CollapsedCommentView : UserControl
    {
        public CollapsedCommentView(ICommentViewModel cvm)
        {
            InitializeComponent();

            DataContext = cvm;
        }
    }
}