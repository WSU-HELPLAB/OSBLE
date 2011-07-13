using System.ComponentModel;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View.DocumentHolder;
using ReviewInterfaceBase.ViewModel.Document;
using ReviewInterfaceBase.ViewModel.FindWindow;

namespace ReviewInterfaceBase.ViewModel.DocumentHolder
{
    public interface IDocumentHolderViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// This sets or gets if it is being displayed
        /// </summary>
        bool IsDisplayed
        {
            get;
            set;
        }

        /// <summary>
        /// This gets the documentID of the Document it is holding
        /// </summary>
        int idOfDocument
        {
            get;
        }

        /// <summary>
        /// Gets the view of the DocumentHolder
        /// </summary>
        /// <returns>the view of the DocumentHolder</returns>
        IDocumentHolderView GetView();

        /// <summary>
        /// This gets the content of the DocumnetHolder
        /// </summary>
        /// <returns></returns>
        FrameworkElement GetContent();

        /// <summary>
        /// This gets the content's scroll viewer
        /// </summary>
        /// <returns></returns>
        FrameworkElement GetContentScrollViewer();

        /// <summary>
        /// This finds the next object that contains the search string according to the options.
        /// It searches document then comments
        /// </summary>
        /// <param name="foundLast">The object that was last found</param>
        /// <param name="options">The options for the search</param>
        /// <returns>the object that was found or null if no object was found</returns>
        object FindNext(object foundLast, FindWindowOptions options);

        /// <summary>
        /// This gets the DocumentViewModel the one that is holding
        /// </summary>
        /// <returns></returns>
        IDocumentViewModel GetDocumentViewModel();

        /// <summary>
        /// This enables comments to be added
        /// </summary>
        void AllowNewComments();

        /// <summary>
        /// This sets up the comments that are associated wit this document
        /// </summary>
        /// <param name="document">Needs to point at Document Tag</param>
        /// <param name="author"></param>
        void LoadIssueVotingComments(XElement document, NoteAuthor author);

        /// <summary>
        /// This writes all the need information to the writer
        /// </summary>
        /// <param name="writer"></param>
        void WriteXml(XmlWriter writer);
    }
}