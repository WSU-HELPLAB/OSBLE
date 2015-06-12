using System.Windows.Controls;

namespace ReviewInterfaceBase.View.Document
{
    public partial class TextDocumentView : UserControl, IDocumentView
    {
        #region Constructor

        /// <summary>
        /// NOT TO BE CALLED EXCEPT BY TextFileDocumentViewModel.  If you want to make a TextFileDocument use
        /// the TextFileDocumentViewModel Constructor
        /// </summary>
        /// <param name="textFileDocumentViewModel"></param>
        public TextDocumentView()
        {
            InitializeComponent();
        }

        #endregion Constructor
    }
}