using ReviewInterfaceBase.ViewModel.FindWindow;

namespace ReviewInterfaceBase.ViewModel.Document
{
    public interface IDocumentViewModel
    {
        int DocumentID
        {
            get;
        }

        object FindNext(object lastFound, FindWindowOptions options);
    }
}