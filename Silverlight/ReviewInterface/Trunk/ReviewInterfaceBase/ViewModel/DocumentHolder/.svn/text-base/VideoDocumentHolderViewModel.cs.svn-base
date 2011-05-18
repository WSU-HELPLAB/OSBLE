using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View.DocumentHolder;
using ReviewInterfaceBase.ViewModel.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;
using ReviewInterfaceBase.ViewModel.Document;
using ReviewInterfaceBase.ViewModel.Document.VideoDocument.VideoPlayer;

namespace ReviewInterfaceBase.ViewModel.DocumentHolder
{
    /// <summary>
    /// Technically this should be a ChronologicalDocumentHolderViewModel that could hold video but since video
    /// is the only thing that Chronological that we are using this is unneeded overhead
    /// </summary>
    public class VideoDocumentHolderViewModel : IDocumentHolderViewModel
    {
        #region Delegates

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion Delegates

        #region Field

        private VideoDocumentHolderView thisView;

        private CommentManipulation commentManipulation;

        private VideoPlayerViewModel videoPlayerVM;

        #endregion Field

        #region Properties

        public bool IsDisplayed
        {
            get
            {
                return videoPlayerVM.IsDisplayed;
            }
            set
            {
                videoPlayerVM.IsDisplayed = value;
                PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayed"));
            }
        }

        public int idOfDocument
        {
            get
            {
                return videoPlayerVM.DocumentID;
            }
        }

        #endregion Properties

        #region Constructor

        public VideoDocumentHolderViewModel(int documentID, Stream stream)
        {
            thisView = new VideoDocumentHolderView(this);

            videoPlayerVM = new VideoPlayerViewModel(documentID);

            thisView.CommentStackPanelHolder.Children.Add(new StackPanel());

            Point location = new Point();

            location.X = (double)thisView.GetValue(Canvas.LeftProperty);
            location.Y = (double)thisView.GetValue(Canvas.TopProperty);

            commentManipulation = new CommentManipulation(videoPlayerVM.DocumentID, this, new Canvas(), thisView.CommentStackPanelHolder, new Grid(), new Grid(), new Grid());

            //because the default is overly but with video default should be on the right side
            commentManipulation.ToggleView();

            commentManipulation.RequestOutlineHighLighting += new EventHandler(commentManipulation_RequestOutlineHighLighting);
            commentManipulation.RequestRemoveHighLighting += new EventHandler(commentManipulation_RequestRemoveHighLighting);
            commentManipulation.RequestSolidHighLighting += new EventHandler(commentManipulation_RequestSolidHighLighting);

            videoPlayerVM.AddNewComment += new RoutedPropertyChangedEventHandler<TimeSpan>(videoPlayerVM_AddNewComment);

            thisView.LayoutRoot.Children.Add(videoPlayerVM.GetView());

            videoPlayerVM.SetSource(stream);
        }

        #endregion Constructor

        #region Public Methods

        public IDocumentViewModel GetDocumentViewModel()
        {
            return videoPlayerVM;
        }

        public FrameworkElement GetContent()
        {
            return null;
        }

        public IDocumentHolderView GetView()
        {
            return thisView;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Document");
            videoPlayerVM.WriteXml(writer);
            writer.WriteStartElement("Comments");
            commentManipulation.WriteXml(writer);

            //end the comments
            writer.WriteEndElement();

            //end the videoDocument
            writer.WriteEndElement();
        }

        public FrameworkElement GetContentScrollViewer()
        {
            return null;
        }

        public void LoadIssueVotingComments(XElement xmlDocument, NoteAuthor author, int peerReviewID)
        {
            foreach (XElement xmlComment in xmlDocument.Descendants("Comments").ElementAt(0).Descendants("Comment"))
            {
                VideoLocation location = videoPlayerVM.GetReferenceLocationFromXml(xmlComment.Descendants("Location").First());
                videoPlayerVM.CreateCommentReference(location.Location);
                commentManipulation.addIssueVotingComment(xmlComment, author, location, peerReviewID);
            }
        }

        public void AllowNewComments()
        {
            videoPlayerVM.AllowNewComments();
        }

        public object FindNext(object foundLast, ReviewInterfaceBase.ViewModel.FindWindow.FindWindowOptions options)
        {
            return null;
        }

        #endregion Public Methods

        #region Private Helpers

        private void commentManipulation_RequestSolidHighLighting(object sender, EventArgs e)
        {
            videoPlayerVM.HighlightCommentReference(((sender as ICommentViewModel).referenceLocation as VideoLocation).Location);
        }

        private void commentManipulation_RequestRemoveHighLighting(object sender, EventArgs e)
        {
            videoPlayerVM.RemoveCommentReference(((sender as ICommentViewModel).referenceLocation as VideoLocation).Location);
        }

        private void commentManipulation_RequestOutlineHighLighting(object sender, EventArgs e)
        {
            videoPlayerVM.RemoveHighlightingCommentReference(((sender as ICommentViewModel).referenceLocation as VideoLocation).Location);
        }

        private void videoPlayerVM_AddNewComment(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            commentManipulation.addNewPeerReviewComment(new VideoLocation(e.NewValue));
        }

        #endregion Private Helpers
    }
}