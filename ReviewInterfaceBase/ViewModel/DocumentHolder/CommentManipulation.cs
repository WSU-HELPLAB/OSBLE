using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View.Comment;
using ReviewInterfaceBase.ViewModel.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.DocumentHolder
{
    /// <summary>
    /// This an enum that says how we are currently manipulating a comment
    /// </summary>
    internal enum Manipulation
    {
        Nothing,
        Moving,
        Resizing
    }

    public class CommentManipulation
    {
        #region Delegates

        /// <summary>
        /// This is fired whenever the comment reference should have solid highlighting
        /// </summary>
        public event EventHandler RequestSolidHighLighting = delegate { };

        /// <summary>
        /// This is fired whenever the comment reference should have Outline Highlighting
        /// </summary>
        public event EventHandler RequestOutlineHighLighting = delegate { };

        /// <summary>
        /// This is fired when the Highlighting needs to be remove completely
        /// </summary>
        public event EventHandler RequestRemoveHighLighting = delegate { };

        /// <summary>
        /// This is fired whenever a comment got focus
        /// </summary>
        public event EventHandler GotFocus = delegate { };

        #endregion Delegates

        #region Fields

        /// <summary>
        /// This says whether or not initial positions have been set.
        /// We need this because if the document is not in view then we cannot set its position
        /// This will come up once for IssueVoting and not at all for Peer Review
        /// </summary>
        private bool? initalPositionsSet = null;

        /// <summary>
        /// The documentID that is associated with this instance of CommentManipulaion
        /// </summary>
        private int documentID;

        /// <summary>
        /// The currently selected comment
        /// </summary>
        private ICommentViewModel _selectedComment = null;

        /// <summary>
        /// The current manipulation to the selected comment
        /// </summary>
        private Manipulation currentManipulation = Manipulation.Nothing;

        /// <summary>
        /// The offset of the content
        /// </summary>
        private Point initialOffset;

        /// <summary>
        /// This is true if the comments are on the canvas over the document
        /// This is false if the comments are in the grid to the right of the document
        /// </summary>
        private bool isFreeFloating = true;

        /// <summary>
        /// This is the list of comments
        /// </summary>
        private ObservableCollection<ICommentViewModel> comments = new ObservableCollection<ICommentViewModel>();

        /// <summary>
        /// This is a reference to the canvas over the document
        /// </summary>
        private Canvas commentCanvasOverlay;

        /// <summary>
        /// This is a reference to the grid that is over the document and used for line placement
        /// </summary>
        private Grid lineCanvasOverlayLeftSide;

        /// <summary>
        /// This is a reference to the grid that is over the gridSplitter and used for line placement
        /// </summary>
        private Grid lineCanvasOverlayMiddle;

        /// <summary>
        /// This is a reference to the grid that is over the grid on the right and used for line placement
        /// </summary>
        private Grid lineCanvasOverlayRightSide;

        /// <summary>
        /// This is the grid that holds the comments on the right side
        /// </summary>
        private Grid commentCommentStackPanelHolder;

        /// <summary>
        /// This is a reference to the documentHolderViewModel
        /// </summary>
        private IDocumentHolderViewModel documentHolderViewModel;

        #endregion Fields

        #region Properties

        /// <summary>
        /// This returns whether or not the comments are free floating.
        /// That is whether or not the comments are over the document
        /// </summary>
        public bool IsFreeFloating
        {
            get { return isFreeFloating; }
            set { isFreeFloating = value; }
        }

        /// <summary>
        /// This is the selectedComment.  It is a private because only CommentManipulatation can change it
        /// It is a property so that we have the code that is required to change a selectedComment in just one place
        /// </summary>
        private ICommentViewModel selectedComment
        {
            get
            {
                return _selectedComment;
            }
            set
            {
                //do nothing if the new value is the same as the old
                if (value != _selectedComment)
                {
                    //otherwise if the new value != null then make it solid
                    if (value != null)
                    {
                        HighlightLine(value);
                        RequestSolidHighLighting(value, EventArgs.Empty);
                    }

                    //if the old value is not null then make it outlined
                    if (_selectedComment != null)
                    {
                        DashLine(_selectedComment);
                        RequestOutlineHighLighting(_selectedComment, EventArgs.Empty);
                    }
                    _selectedComment = value;
                }
            }
        }

        /// <summary>
        /// This is a list of all the comments made
        /// </summary>
        public ObservableCollection<ICommentViewModel> Comments
        {
            get { return comments; }
            set { comments = value; }
        }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// This is the constructor for CommentManpulation, no parameter can be null
        /// </summary>
        /// <param name="documentID">The documentID of the DocumentHolderViewModel</param>
        /// <param name="DocumentHolderViewModel">A reference to the DocumentHolderViewModel</param>
        /// <param name="CommentCanvasOverlay">A reference to the canvas that is over the document </param>
        /// <param name="CommentCommentStackPanelHolder">A reference to the Grid that is to the right of the  document</param>
        /// <param name="LineCanvasOverlayLeftSide">A reference to the grid over the document use to hold lines</param>
        /// <param name="LineCanvasOverlayMiddle">A reference to the grid over the grid splitter use to hold lines</param>
        /// <param name="LineCanvasOverlayRightSide">A reference to the grid that is over the grid on the right side of the document</param>
        public CommentManipulation(int documentID, IDocumentHolderViewModel DocumentHolderViewModel, Canvas CommentCanvasOverlay, Grid CommentCommentStackPanelHolder, Grid LineCanvasOverlayLeftSide, Grid LineCanvasOverlayMiddle, Grid LineCanvasOverlayRightSide)
        {
            this.documentID = documentID;
            documentHolderViewModel = DocumentHolderViewModel;
            commentCanvasOverlay = CommentCanvasOverlay;
            commentCommentStackPanelHolder = CommentCommentStackPanelHolder;
            lineCanvasOverlayLeftSide = LineCanvasOverlayLeftSide;
            lineCanvasOverlayMiddle = LineCanvasOverlayMiddle;
            lineCanvasOverlayRightSide = LineCanvasOverlayRightSide;
        }

        #endregion Constructor

        #region Members

        /// <summary>
        /// This hides all the lines
        /// </summary>
        public void HideLines()
        {
            foreach (ICommentViewModel cvm in comments)
            {
                RemoveLine(cvm);
            }
        }

        /// <summary>
        /// This updates all the lines
        /// </summary>
        public void UpdateLines()
        {
            foreach (ICommentViewModel cvm in comments)
            {
                DrawLine(cvm);
            }
        }

        /// <summary>
        /// This find the next comment containing the search string
        /// </summary>
        /// <param name="lastComment">The last comment the search string was found in</param>
        /// <param name="searchString">The string that is to be found</param>
        /// <returns>null if no such comment was found else a reference to the comment</returns>
        public ICommentViewModel FindNextComment(ICommentViewModel lastComment, string searchString)
        {
            int startIndex = 0;
            if (lastComment != null)
            {
                //plus one because we want start looking at the next comment
                startIndex = comments.IndexOf(lastComment) + 1;
            }

            while (startIndex < comments.Count)
            {
                if (comments[startIndex].NoteText.Contains(searchString))
                {
                    return comments[startIndex];
                }
                startIndex++;
            }
            return null;
        }

        /// <summary>
        /// This writes all the comments to XML
        /// </summary>
        /// <param name="writer">writer</param>
        public void WriteXml(XmlWriter writer)
        {
            //For each comment we have it write itself to the writer
            foreach (ICommentViewModel comment in comments)
            {
                comment.XmlWrite(writer);
            }
        }

        /// <summary>
        /// This hides all the comments that do contain the string in text
        /// </summary>
        /// <param name="text">the string to search for</param>
        /// <param name="isCaseSensitive">indicates whether or not case should considered</param>
        public void DisplayOnlyCommentsWithText(string text, bool isCaseSensitive)
        {
            if (!isCaseSensitive)
            {
                text = text.ToLower();
                foreach (ICommentViewModel commentViewModel in comments)
                {
                    if (commentViewModel.NoteText.ToLower().Contains(text))
                    {
                        AddToUIElement(commentViewModel);
                    }
                    else
                    {
                        RemoveFromUIElement(commentViewModel);
                    }
                }
            }
            else
            {
                foreach (ICommentViewModel commentViewModel in comments)
                {
                    if (commentViewModel.NoteText.Contains(text))
                    {
                        AddToUIElement(commentViewModel);
                    }
                    else
                    {
                        RemoveFromUIElement(commentViewModel);
                    }
                }
            }
        }

        /// <summary>
        /// This hides all the comments
        /// </summary>
        public void HideAll()
        {
            foreach (ICommentViewModel commentViewModel in comments)
            {
                RemoveFromUIElement(commentViewModel);
            }
        }

        /// <summary>
        /// This switches the view between free floating on in the right grid.
        /// </summary>
        public void ToggleView()
        {
            if (isFreeFloating)
            {
                isFreeFloating = !isFreeFloating;
                commentCanvasOverlay.Children.Clear();
                foreach (ICommentViewModel comment in comments)
                {
                    comment.UsingView = true;
                    AddToUIElement(comment);
                }
            }
            else
            {
                isFreeFloating = !isFreeFloating;
                foreach (StackPanel sp in commentCommentStackPanelHolder.Children)
                {
                    sp.Children.Clear();
                }
                foreach (ICommentViewModel comment in comments)
                {
                    comment.UsingView = true;
                    AddToUIElement(comment);
                }
            }
        }

        /// <summary>
        /// This adds a new Peer Review Comment
        /// </summary>
        /// <param name="location">A reference to the VideoLocation reference</param>
        public void addNewPeerReviewComment(VideoLocation location)
        {
            //create ViewModel (this in turn makes the View)
            ICommentViewModel commentViewModel = new PeerReviewCommentView(documentID, location).ViewModel;

            initilizeVideoComment(commentViewModel, location);
        }

        /// <summary>
        /// This adds a 'new' issue voting comment from xml
        /// </summary>
        /// <param name="xmlComment">takes a XElement which needs to be pointing to the Comment tag</param>
        /// <param name="author">takes a reference to an instance of NoteAuthor</param>
        /// <param name="location">Takes a reference to the videoLocation reference</param>
        public void addIssueVotingComment(XElement xmlComment, NoteAuthor author, VideoLocation location)
        {
            //create ViewModel (this in turn makes the View)
            ICommentViewModel commentViewModel = new IssueVotingCommentViewModel(xmlComment, author, location);
            initilizeVideoComment(commentViewModel, location);
        }

        /// <summary>
        /// This adds a new NoteText
        /// </summary>
        /// <param name="location">location of the text that the noteText is commenting on</param>
        /// <param name="ContentStart">Start of RichTextBox content</param>
        /// <param name="ContentEnd">End of RichTextBox content</param>
        public void addNewPeerReviewComment(ISpatialLocation location, Size documentSize)
        {
            //create ViewModel (this in turn makes the View)
            ICommentViewModel commentViewModel = new PeerReviewCommentView(documentID, location).ViewModel;

            initializeSpatialComment(commentViewModel, documentSize);
        }

        /// <summary>
        /// This adds a NoteText from an xmlComment
        /// </summary>
        /// <param name="xmlComment">an XElement of a comment</param>
        public void addSavedPeerReviewComment(XElement xmlComment, ISpatialLocation location, Size documentSize)
        {
            //create ViewModel (this in turn makes the View)
            ICommentViewModel commentViewModel = new PeerReviewCommentView(documentID, location).ViewModel;

            //Setting the boolean value of SavedComment to true so that PeerReviewCommentViewModel will know how to handle it
            (commentViewModel as PeerReviewCommentViewModel).LoadedComment = true;

            initializeSpatialComment(commentViewModel, documentSize);

            //The last comment in the observable collection "comments" will be the one that was just added. So we can apply the note for the comment from the xml
            comments[comments.Count - 1].NoteText = xmlComment.Attribute("NoteText").Value;

            //Setting the value of XMLCategory to the portion of the xmlComment that has the categories
            (commentViewModel as PeerReviewCommentViewModel).XMLCategory = xmlComment.Descendants("Categories").ElementAt(0);
        }

        /// <summary>
        /// This adds a new NoteText
        /// </summary>
        /// <param name="location">location of the text that the noteText is commenting on</param>
        /// <param name="ContentStart">Start of RichTextBox content</param>
        /// <param name="ContentEnd">End of RichTextBox content</param>
        public void addIssueVotingComment(XElement xmlComment, NoteAuthor author, ISpatialLocation location, Size documentSize)
        {
            //create ViewModel (this in turn makes the View)
            ICommentViewModel commentViewModel = new IssueVotingCommentViewModel(xmlComment, author, location);
            initializeSpatialComment(commentViewModel, documentSize);
        }

        /// <summary>
        /// This adds a new read-only(has no issue voting options) Notetext
        /// </summary>
        /// <param name="location">location of the text that the noteText is commenting on</param>
        /// <param name="ContentStart">Start of RichTextBox content</param>
        /// <param name="ContentEnd">End of RichTextBox content</param>
        public void addIssueVotingCommentWithoutIssueVoting(XElement xmlComment, NoteAuthor author, ISpatialLocation location, Size documentSize)
        {
            //create ViewModel (this in turn makes the View)
            ICommentViewModel commentViewModel = new ViewPeerReviewCommentViewModel(xmlComment, author, location);
            initializeSpatialComment(commentViewModel, documentSize);
        }

        /// <summary>
        /// This shows all the comments
        /// </summary>
        public void ShowAll()
        {
            foreach (ICommentViewModel commentViewModel in comments)
            {
                if (commentViewModel.GetView().Parent == null)
                {
                    AddToUIElement(commentViewModel);
                }
            }
        }

        #endregion Members

        #region Private Helpers

        /// <summary>
        /// This initilizesVideoComment.  It sets up the handlers, inserts it into the list, and displays it
        /// </summary>
        /// <param name="commentViewModel">CommentViewModel</param>
        /// <param name="location">VideoLocation</param>
        private void initilizeVideoComment(ICommentViewModel commentViewModel, VideoLocation location)
        {
            //set listeners
            commentViewModel.Moving += new MouseButtonEventHandler(commentViewModel_StartMove);
            commentViewModel.Remove += new EventHandler(commentViewModel_Remove);
            commentViewModel.Resizing += new MouseButtonEventHandler(commentViewModel_StartResize);
            commentViewModel.Minimize += new MouseButtonEventHandler(commentViewModel_Minimize);
            commentViewModel.Maximize += new EventHandler(commentViewModel_Maximize);
            commentViewModel.LostFocus += new RoutedEventHandler(commentViewModel_LostFocus);
            commentViewModel.GotFocus += new RoutedEventHandler(commentViewModel_GotFocus);
            commentViewModel.SizeChanged += new EventHandler(commentViewModel_SizeChanged);

            commentViewModel.GetView().LostMouseCapture += new MouseEventHandler(CommentManipulation_LostMouseCapture);

            commentViewModel.Header = HelperClass.ConvertTimeSpanToString(location.Location);

            //make this the selectedComment but we are not manipulating it
            selectedComment = commentViewModel;
            currentManipulation = Manipulation.Nothing;

            int i = 0;
            foreach (ICommentViewModel cvm in comments)
            {
                if ((cvm.referenceLocation as VideoLocation).Location > location.Location)
                {
                    break;
                }
                i++;
            }

            comments.Insert(i, commentViewModel);

            AddToUIElement(commentViewModel);

            //give it focus must be done after it has been added to the UI
            //commentViewModel.GetView().Note.Focus();
        }

        /// <summary>
        /// This handles what should happen if the comments size has been changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentViewModel_SizeChanged(object sender, EventArgs e)
        {
            ICommentViewModel cvm = sender as ICommentViewModel;
            //cvm.Size = new Size(e.NewSize.Width, e.NewSize.Height);
            setCommentLocation(cvm, new Point(0, 0));
            DrawLine(cvm);
        }

        /// <summary>
        /// This initialize a spatial comment.  It sets up the handlers, inserts it into the list, and displays it
        /// </summary>
        /// <param name="commentViewModel"></param>
        /// <param name="documentSize"></param>
        private void initializeSpatialComment(ICommentViewModel commentViewModel, Size documentSize)
        {
            ILocation location = commentViewModel.referenceLocation;

            //set listeners
            commentViewModel.Moving += new MouseButtonEventHandler(commentViewModel_StartMove);
            commentViewModel.Remove += new EventHandler(commentViewModel_Remove);
            commentViewModel.Resizing += new MouseButtonEventHandler(commentViewModel_StartResize);
            commentViewModel.Minimize += new MouseButtonEventHandler(commentViewModel_Minimize);
            commentViewModel.Maximize += new EventHandler(commentViewModel_Maximize);
            commentViewModel.LostFocus += new RoutedEventHandler(commentViewModel_LostFocus);
            commentViewModel.GotFocus += new RoutedEventHandler(commentViewModel_GotFocus);
            commentViewModel.GetView().LostMouseCapture += new MouseEventHandler(CommentManipulation_LostMouseCapture);

            if (location is TextLocation)
            {
                TextLocation dl = location as TextLocation;
                if (dl.StartLineAndIndex.Item1 == dl.EndLineAndIndex.Item1)
                {
                    //plus one because it starts at 0 but that is now how people normally count
                    commentViewModel.Header = "On Line: " + (dl.EndLineAndIndex.Item1 + 1).ToString() + "  ";
                }
                else
                {
                    //plus one because it starts at 0 but that is now how people normally count
                    commentViewModel.Header = "On Lines " + (dl.StartLineAndIndex.Item1 + 1).ToString() + "-" + (dl.EndLineAndIndex.Item1 + 1).ToString() + "  ";
                }
            }

            //make this the selectedComment but we are not manipulating it
            selectedComment = commentViewModel;
            currentManipulation = Manipulation.Nothing;

            if (documentHolderViewModel.IsDisplayed)
            {
                setCommentInitalLocation(commentViewModel);
                initalPositionsSet = true;
            }
            else
            {
                if (initalPositionsSet == null)
                {
                    documentHolderViewModel.PropertyChanged += new PropertyChangedEventHandler(documentHolderViewModel_PropertyChanged);
                    initalPositionsSet = false;
                }
            }

            //If it is XpsLocation then we keep an ordered list
            if (location is XpsLocation)
            {
                int i = 0;
                List<Rectangle> commentViewModelRects = (commentViewModel.referenceLocation as XpsLocation).Rectangles;
                foreach (ICommentViewModel cvm in comments)
                {
                    List<Rectangle> cvmRects = (cvm.referenceLocation as XpsLocation).Rectangles;

                    if ((double)(cvmRects[cvmRects.Count - 1].GetValue(Canvas.TopProperty)) > (double)(commentViewModelRects[commentViewModelRects.Count - 1].GetValue(Canvas.TopProperty)))
                    {
                        break;
                    }
                    i++;
                }
                comments.Insert(i, commentViewModel);
            }
            else
            {
                comments.Add(commentViewModel);
            }

            AddToUIElement(commentViewModel);

            //give it focus must be done after it has been added to the UI
            //commentViewModel.GetView().Note.Focus();
        }

        private void documentHolderViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDisplayed" && initalPositionsSet == false && documentHolderViewModel.IsDisplayed == true)
            {
                //Ok so this a little bit of hack... IsDisplayed is changed when the user clicks for it to be displayed not after Layout has been updated
                //which to get the realLocation of the object we need to it to be really displayed so we got to wait for LayoutRoot to be updated.
                //So this is a little bit of a hack to get
                (documentHolderViewModel.GetView() as UserControl).LayoutUpdated += new EventHandler(CommentManipulation_LayoutUpdated);

                initalPositionsSet = true;
            }
        }

        private void CommentManipulation_LayoutUpdated(object sender, EventArgs e)
        {
            //We only want to set their locations once so stop listening
            (documentHolderViewModel.GetView() as UserControl).LayoutUpdated -= new EventHandler(CommentManipulation_LayoutUpdated);

            foreach (ICommentViewModel cvm in comments)
            {
                setCommentInitalLocation(cvm);
            }
        }

        private void setCommentInitalLocation(ICommentViewModel commentViewModel)
        {
            Point commentLocation = new Point();

            int count = commentViewModel.SnippetHighlighting.Count - 1;
            FrameworkElement lastObject = commentViewModel.SnippetHighlighting[count];

            Point TopLeft = GetRealLocation(commentViewModel.SnippetHighlighting[0]);
            Point BottemRight = GetRealLocation(lastObject);

            BottemRight.X += lastObject.ActualWidth;
            BottemRight.Y += lastObject.ActualHeight;

            //set the default position of the noteText relative to where the selected text is
            commentLocation.X = TopLeft.X;

            //try to set the noteText above the selected text.
            double newTopLocation = TopLeft.Y - commentViewModel.Height;

            //if it is above the start of the content (probably goes out of the RichTextBox)
            if (newTopLocation < 0)//.GetCharacterRect(LogicalDirection.Forward).Top)
            {
                //set it below the selected text
                newTopLocation = BottemRight.Y;
            }

            //if it is below the bottom of the Content
            if (newTopLocation > TopLeft.Y)//.GetCharacterRect(LogicalDirection.Backward).Top)
            {
                //set it so it starts at the top line of the selected text
                newTopLocation = TopLeft.Y;
            }

            commentLocation.Y = newTopLocation;

            commentViewModel.Location = commentLocation;
        }

        /// <summary>
        /// This goes from a minimize comment to a maximized comment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentViewModel_Maximize(object sender, EventArgs e)
        {
            CollapsedCommentView collapsedView = (sender as ICommentViewModel).GetCollapsedView();
            StackPanel stackPanel = (collapsedView.Parent as StackPanel);

            if (!isFreeFloating)
            {
                //get the index of the coallapsedView
                int StackPanelIndex = stackPanel.Children.IndexOf(collapsedView);

                stackPanel.Children.RemoveAt(StackPanelIndex);

                stackPanel.Children.Insert(StackPanelIndex, (sender as ICommentViewModel).GetView());
                (sender as ICommentViewModel).UsingView = true;
            }
        }

        /// <summary>
        /// This sets where the comment should appear in the freeFloating mode.  This function checks the bounds of
        /// the document and makes sure it is within them
        /// </summary>
        /// <param name="commentViewModel"></param>
        /// <param name="desiredOffset">Where you would like the Top Left of the comment to be</param>
        private void setCommentLocation(ICommentViewModel commentViewModel, Point desiredOffset)
        {
            if ((commentViewModel.GetView().Parent as FrameworkElement) != null)
            {
                double parentHeight = (commentViewModel.GetView().Parent as FrameworkElement).ActualHeight;
                double parentWidth = (commentViewModel.GetView().Parent as FrameworkElement).ActualWidth;

                //This section stops the noteText from going of the screen.
                if (commentViewModel.Location.X + desiredOffset.X < 0)
                {
                    desiredOffset.X = -commentViewModel.Location.X;
                }
                else if (desiredOffset.X + commentViewModel.Location.X + commentViewModel.Size.Width > parentWidth)
                {
                    desiredOffset.X = parentWidth - commentViewModel.Size.Width - commentViewModel.Location.X;
                }

                if (commentViewModel.Location.Y + desiredOffset.Y < 0)
                {
                    desiredOffset.Y = -commentViewModel.Location.Y;
                }
                else if (desiredOffset.Y + commentViewModel.Location.Y + commentViewModel.Size.Height > parentHeight)
                {
                    desiredOffset.Y = parentHeight - commentViewModel.Size.Height - commentViewModel.Location.Y;
                }

                commentViewModel.Location = new Point(
                    commentViewModel.Location.X + desiredOffset.X, commentViewModel.Location.Y + desiredOffset.Y);
            }
        }

        /// <summary>
        /// This makes the line solid
        /// </summary>
        /// <param name="cvm"></param>
        private void HighlightLine(ICommentViewModel cvm)
        {
            foreach (Line line in cvm.SnippetToCommentLine)
            {
                line.StrokeDashArray = null;
            }
        }

        /// <summary>
        /// This makes the line dashed
        /// </summary>
        /// <param name="cvm"></param>
        private void DashLine(ICommentViewModel cvm)
        {
            foreach (Line line in cvm.SnippetToCommentLine)
            {
                DoubleCollection dc = new DoubleCollection();
                dc.Add(2);
                dc.Add(2);
                line.StrokeDashArray = dc;
            }
        }

        /// <summary>
        /// This removes a comment from being displayed note that the comment is not delete just cannot be seen
        /// </summary>
        /// <param name="commentViewModel"></param>
        private void RemoveFromUIElement(ICommentViewModel commentViewModel)
        {
            if (isFreeFloating)
            {
                if (commentViewModel.UsingView)
                {
                    commentCanvasOverlay.Children.Remove(commentViewModel.GetView());
                }
                else
                {
                    commentCanvasOverlay.Children.Remove(commentViewModel.GetCollapsedView());
                }
            }
            else
            {
                if (commentViewModel.UsingView)
                {
                    if (commentViewModel.referenceLocation is TextLocation)
                    {
                        (commentCommentStackPanelHolder.
                        Children[(commentViewModel.referenceLocation as TextLocation).EndLineAndIndex.Item1] as StackPanel).
                        Children.Remove(commentViewModel.GetView());
                    }
                    else if (commentViewModel.referenceLocation is VideoLocation)
                    {
                        (commentCommentStackPanelHolder.
                        Children[0] as StackPanel).
                        Children.Remove(commentViewModel.GetView());
                    }
                }
                else
                {
                    if (commentViewModel.referenceLocation is TextLocation)
                    {
                        (commentCommentStackPanelHolder.
                        Children[0] as StackPanel).
                        Children.Remove(commentViewModel.GetCollapsedView());
                    }
                    else if (commentViewModel.referenceLocation is VideoLocation)
                    {
                    }
                }
            }
            RemoveLine(commentViewModel);
        }

        /// <summary>
        /// This adds a comment so it can be sign not this doesn't do anything back end stuff.
        /// </summary>
        /// <param name="commentViewModel"></param>
        private void AddToUIElement(ICommentViewModel commentViewModel)
        {
            if (isFreeFloating)
            {
                if (commentViewModel.UsingView)
                {
                    if (commentViewModel.GetView().Parent == null)
                    {
                        commentCanvasOverlay.Children.Add(commentViewModel.GetView());
                    }
                }
                else
                {
                    if (commentViewModel.GetCollapsedView().Parent == null)
                    {
                        commentCanvasOverlay.Children.Add(commentViewModel.GetCollapsedView());
                    }
                }
            }
            else
            {
                if (commentViewModel.UsingView)
                {
                    if (commentViewModel.GetView().Parent == null)
                    {
                        //DocumentLocation is 'special' as order matters
                        if (commentViewModel.referenceLocation is TextLocation)
                        {
                            (commentCommentStackPanelHolder.
                                Children[(commentViewModel.referenceLocation as TextLocation).EndLineAndIndex.Item1] as StackPanel).
                                Children.Add(commentViewModel.GetView());
                        }
                        else
                        {
                            int index = comments.IndexOf(commentViewModel);
                            (commentCommentStackPanelHolder.Children[0] as StackPanel).Children.Insert(index, commentViewModel.GetView());
                        }
                    }
                }
                else
                {
                    if (commentViewModel.GetCollapsedView().Parent == null)
                    {
                        if (commentViewModel.referenceLocation is TextLocation)
                        {
                            (commentCommentStackPanelHolder.
                                Children[(commentViewModel.referenceLocation as TextLocation).EndLineAndIndex.Item1] as StackPanel).
                                Children.Add(commentViewModel.GetCollapsedView());
                        }
                        else
                        {
                            int index = comments.IndexOf(commentViewModel);
                            (commentCommentStackPanelHolder.Children[0] as StackPanel).Children.Insert(index, commentViewModel.GetCollapsedView());
                        }
                    }
                }
            }

            DrawLine(commentViewModel);
        }

        /// <summary>
        /// This removes all the lines associated with the commnetVieModel.
        /// </summary>
        /// <param name="commentViewModel"></param>
        private void RemoveLine(ICommentViewModel commentViewModel)
        {
            if (commentViewModel != null)
            {
                foreach (Line line in commentViewModel.SnippetToCommentLine)
                {
                    commentCanvasOverlay.Children.Remove(line);
                    lineCanvasOverlayLeftSide.Children.Remove(line);
                    lineCanvasOverlayMiddle.Children.Remove(line);
                    lineCanvasOverlayRightSide.Children.Remove(line);
                }
                commentViewModel.SnippetToCommentLine.Clear();
            }
        }

        /// <summary>
        /// This gets the 'real location' of the FrameworkElement
        /// Note: this may not work perfectly especially if the object has been transformed
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Point GetRealLocation(FrameworkElement obj)
        {
            if (isFreeFloating)
            {
                try
                {
                    var transform = obj.TransformToVisual(documentHolderViewModel.GetContent() as FrameworkElement);
                    return transform.Transform(new Point(0, 0));
                }
                catch
                {
                    return new Point();
                }
            }
            else
            {
                try
                {
                    var transform = obj.TransformToVisual(documentHolderViewModel.GetContentScrollViewer() as FrameworkElement);
                    return transform.Transform(new Point(0, 0));
                }
                catch
                {
                    return new Point();
                }
            }
        }

        /// <summary>
        /// This draws the lines need for the provided commentViewModel
        /// </summary>
        /// <param name="commentViewModel"></param>
        private void DrawLine(ICommentViewModel commentViewModel)
        {
            if (commentViewModel != null)
            {
                RemoveLine(commentViewModel);

                //if neither views have a parent then we don't have to do anything
                if (commentViewModel.GetView().Parent != null || commentViewModel.GetCollapsedView().Parent != null)
                {
                    if (commentViewModel.SnippetHighlighting != null && commentViewModel.SnippetHighlighting.Count > 0)
                    {
                        int count = commentViewModel.SnippetHighlighting.Count;

                        if (isFreeFloating)
                        {
                            FrameworkElement fwe = commentViewModel.SnippetHighlighting[count - 1];
                            Point objectLocation = GetRealLocation(fwe);
                            Size objectSize = new Size(fwe.ActualWidth, fwe.ActualHeight);

                            Point p = (fwe.Parent as FrameworkElement).RenderTransform.Transform(new Point(objectSize.Width, objectSize.Height));

                            objectSize.Width = p.X;
                            objectSize.Height = p.Y;

                            Point commentLocation = commentViewModel.Location;
                            Size commentSize = commentViewModel.Size;

                            objectLocation = toEdgeOfObject(commentLocation, objectLocation, commentSize, objectSize, 0);

                            Line newLine = new Line();

                            newLine.X1 = objectLocation.X;
                            newLine.Y1 = objectLocation.Y;
                            newLine.X2 = commentLocation.X + commentSize.Width / 2;
                            newLine.Y2 = commentLocation.Y + commentSize.Height / 2;

                            commentViewModel.SnippetToCommentLine.Add(newLine);

                            setLineProperties(commentViewModel);

                            //We set it at the first element to make sure all lines are behind all comments
                            commentCanvasOverlay.Children.Insert(0, newLine);
                        }
                        else
                        {
                            RemoveLine(commentViewModel);

                            bool addRightLine = true;

                            Point textLocation = GetRealLocation(commentViewModel.SnippetHighlighting[count - 1]);

                            //first line, line from reference to grid splitter
                            Line leftLine = new Line();

                            FrameworkElement fwe = commentViewModel.SnippetHighlighting[count - 1];

                            Size fweSize = new Size(fwe.ActualWidth, fwe.ActualHeight);

                            Point p = (fwe.Parent as FrameworkElement).RenderTransform.Transform(new Point(fweSize.Width, fweSize.Height));

                            leftLine.X1 = textLocation.X + p.X;
                            leftLine.Y1 = textLocation.Y + p.Y;

                            leftLine.X2 = lineCanvasOverlayLeftSide.ActualWidth;
                            leftLine.Y2 = leftLine.Y1;

                            //second line, line from left side of grid splitter to right side of grid splitter

                            Line middleLine = new Line();

                            middleLine.X1 = 0;
                            middleLine.Y1 = leftLine.Y2;

                            middleLine.X2 = lineCanvasOverlayMiddle.ActualWidth;
                            middleLine.Y2 = middleLine.Y1;

                            //third line, line from right side of grid splitter to left side of comment

                            Line rightLine = new Line();

                            rightLine.X1 = 0;
                            rightLine.Y1 = middleLine.Y2;

                            Point viewLocation = new Point();

                            try
                            {
                                if (commentViewModel.UsingView)
                                {
                                    GeneralTransform ViewTransform = commentViewModel.GetView().TransformToVisual(Application.Current.RootVisual as UIElement);
                                    viewLocation = ViewTransform.Transform(new Point(0, 0));
                                }
                                else
                                {
                                    GeneralTransform collapsedViewTransform = commentViewModel.GetCollapsedView().TransformToVisual(Application.Current.RootVisual as UIElement);
                                    viewLocation = collapsedViewTransform.Transform(new Point(0, 0));
                                }
                            }
                            catch
                            {
                                addRightLine = false;
                            }

                            GeneralTransform lineCanvasOverlayRightSideTransform = lineCanvasOverlayRightSide.TransformToVisual(Application.Current.RootVisual as UIElement);
                            Point lineCanvasOverlayRightSideLocation = lineCanvasOverlayRightSideTransform.Transform(new Point(0, 0));

                            rightLine.X2 = viewLocation.X - lineCanvasOverlayRightSideLocation.X;
                            rightLine.Y2 = viewLocation.Y - lineCanvasOverlayRightSideLocation.Y;

                            double DocumentTop = (double)(documentHolderViewModel.GetView() as UIElement).GetValue(Canvas.TopProperty);

                            if (rightLine.Y2 < DocumentTop && rightLine.Y1 < DocumentTop)
                            {
                                addRightLine = false;
                            }
                            else if (rightLine.Y2 < DocumentTop && rightLine.Y1 > DocumentTop)
                            {
                                rightLine.Y2 = (double)(documentHolderViewModel.GetView() as UIElement).GetValue(Canvas.TopProperty);
                            }
                            else if (rightLine.Y1 < DocumentTop && rightLine.Y2 > DocumentTop)
                            {
                                rightLine.Y1 = (double)(documentHolderViewModel.GetView() as UIElement).GetValue(Canvas.TopProperty);
                            }

                            if (rightLine.X2 < rightLine.X1)
                            {
                                rightLine.X2 = rightLine.X1;
                            }

                            //done with making the lines now set properties and add them
                            commentViewModel.SnippetToCommentLine.Add(leftLine);
                            commentViewModel.SnippetToCommentLine.Add(middleLine);

                            if (addRightLine)
                            {
                                commentViewModel.SnippetToCommentLine.Add(rightLine);
                            }

                            leftLine.SetValue(Canvas.ZIndexProperty, 255);
                            middleLine.SetValue(Canvas.ZIndexProperty, 255);
                            rightLine.SetValue(Canvas.ZIndexProperty, 255);

                            setLineProperties(commentViewModel);

                            lineCanvasOverlayLeftSide.Children.Add(leftLine);
                            lineCanvasOverlayMiddle.Children.Add(middleLine);

                            if (addRightLine)
                            {
                                lineCanvasOverlayRightSide.Children.Add(rightLine);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This calculates the intersection point between the stem and the edge of the destination object and
        /// sets the end of the stem to the intersection point.
        /// It works don't touch it
        /// </summary>
        /// <param name="source">This is the source of the stream</param>
        /// <param name="destination">This is destination it assumes length is subtracted from this side</param>
        /// <param name="length">Finds a new point this much closer to source from the intersection point</param>
        private Point toEdgeOfObject(Point TopLeftOfSource, Point TopLeftOfDest, Size SizeOfSource, Size SizeOfDest, double length)
        {
            //            Point TopLeftOfSource = new Point((double)source.GetValue(Canvas.LeftProperty), (double)source.GetValue(Canvas.TopProperty));
            //          Point TopLeftOfDest = new Point((double)destination.GetValue(Canvas.LeftProperty), (double)destination.GetValue(Canvas.TopProperty));
            Point MidOfSource = new Point(TopLeftOfSource.X + SizeOfSource.Width / 2, TopLeftOfSource.Y + SizeOfSource.Height / 2);
            Point MidOfDest = new Point(TopLeftOfDest.X + SizeOfDest.Width / 2, TopLeftOfDest.Y + SizeOfDest.Height / 2);
            Point DistBetweenSandD = new Point(MidOfDest.X - MidOfSource.X, MidOfDest.Y - MidOfSource.Y);
            Point Intersection = new Point();
            Point EndOfStem = new Point();

            double angle;
            double corner = Math.Atan((SizeOfDest.Height / 2) / (SizeOfDest.Width / 2));
            /*
             * Source is in the middle on this graph and the destination goes around it when it does DistBetweenSandD.X and .Y change signs as follows:
                               |
                      -X  -Y   |  +X  -Y
                        -------|--------
                               |
                       -X  +Y  | +X   +Y
                               |
            */

            if (DistBetweenSandD.X > 0 && DistBetweenSandD.Y > 0)
            {
                //So destination is down and to the right of source
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                /*      S
                 *      |\
                 *      |A\
                 *      |  \
                 * B    |   \
                 *      |    \
                 *      |   F \ Intersection Point
                 *      |----|-\---|
                 *    E |____|__\  |
                 *         C |   D |
                 *           |-----|
                 *    S shows where the middle point of the Source object is and D shows the middle point of the Destination Object.
                 *    A is the angle we are using to calculate the intersection point
                 *    The side labeled B is the distBetweenSandD.Y and C is the distBetweenSandD.X
                 *    Using Tan with can find the angle A which is the variable angle
                 *    Then we can find the short side labeled E because it is D's height / 2.
                 *    Then using that and angle A we can find Side F which starts at side B and goes horizontal till it hits the line
                 *    Then we can find the intersection point and do a similar thing again for the arrow / circle size and bob's your uncle
                 *
                 */

                angle = Math.Atan(DistBetweenSandD.X / DistBetweenSandD.Y);

                //got to fix the angle because we got to translate to the other side of the object
                angle = Math.PI / 2 - angle;

                if (angle > corner)
                {
                    Intersection.Y = MidOfDest.Y - SizeOfDest.Height / 2;
                    Intersection.X = MidOfDest.X - (MidOfDest.Y - Intersection.Y) / Math.Tan(angle);
                }
                else if (angle < corner)
                {
                    Intersection.X = MidOfDest.X - SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y - (MidOfDest.X - Intersection.X) * Math.Tan(angle);
                }
                else
                {
                    Intersection.X = MidOfDest.X - SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y - SizeOfDest.Height / 2;
                    EndOfStem.Y = Intersection.Y - (Math.Cos(angle) * length);
                    EndOfStem.X = Intersection.X - (Math.Sin(angle) * length);
                }
                return Intersection;
            }
            else if (DistBetweenSandD.X > 0 && DistBetweenSandD.Y < 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                angle = Math.Atan(DistBetweenSandD.Y / DistBetweenSandD.X);

                if (angle < corner)
                {
                    Intersection.X = MidOfDest.X - SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y + (MidOfDest.X - Intersection.X) * Math.Tan(angle);
                }
                else if (angle > corner)
                {
                    Intersection.Y = MidOfDest.Y + SizeOfDest.Height / 2;
                    Intersection.X = MidOfDest.X - (Intersection.Y - MidOfDest.Y) / Math.Tan(angle);
                }
                else
                {
                    Intersection.X = MidOfDest.X - SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y + SizeOfDest.Height / 2;
                    EndOfStem.Y = Intersection.Y + (Math.Sin(angle) * length);
                    EndOfStem.X = Intersection.X - (Math.Cos(angle) * length);
                }
                return Intersection;
            }

            else if (DistBetweenSandD.X < 0 && DistBetweenSandD.Y > 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                angle = Math.Atan(DistBetweenSandD.Y / DistBetweenSandD.X);

                if (angle < corner)
                {
                    Intersection.X = MidOfDest.X + SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y - (Intersection.X - MidOfDest.X) * Math.Tan(angle);
                }
                else if (angle > corner)
                {
                    Intersection.Y = MidOfDest.Y - SizeOfDest.Height / 2;
                    Intersection.X = MidOfDest.X + (MidOfDest.Y - Intersection.Y) / Math.Tan(angle);
                }
                else
                {
                    Intersection.X = MidOfDest.X + SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y - SizeOfDest.Height / 2;
                    EndOfStem.Y = Intersection.Y - (Math.Cos(angle) * length);
                    EndOfStem.X = Intersection.X + (Math.Sin(angle) * length);
                }
                return Intersection;
            }

            else if (DistBetweenSandD.X < 0 && DistBetweenSandD.Y < 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                angle = Math.Atan(DistBetweenSandD.Y / DistBetweenSandD.X);
                if (angle < corner)
                {
                    Intersection.X = MidOfDest.X + SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y + (Intersection.X - MidOfDest.X) * Math.Tan(angle);
                }
                else if (angle > corner)
                {
                    Intersection.Y = MidOfDest.Y + SizeOfDest.Height / 2;
                    Intersection.X = MidOfDest.X + (Intersection.Y - MidOfDest.Y) / Math.Tan(angle);
                }
                else
                {
                    Intersection.X = MidOfDest.X + SizeOfDest.Width / 2;
                    Intersection.Y = MidOfDest.Y + SizeOfDest.Height / 2;
                    EndOfStem.Y = Intersection.Y + (Math.Sin(angle) * length);
                    EndOfStem.X = Intersection.X + (Math.Cos(angle) * length);
                }
                return Intersection;
            }
            else if (DistBetweenSandD.X == 0 && DistBetweenSandD.Y > 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                EndOfStem.X = MidOfSource.X;
                EndOfStem.Y = MidOfSource.Y + (DistBetweenSandD.Y - (SizeOfDest.Height / 2 + length));
            }
            else if (DistBetweenSandD.X == 0 && DistBetweenSandD.Y < 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                EndOfStem.X = MidOfSource.X;
                EndOfStem.Y = MidOfSource.Y - (DistBetweenSandD.Y - (SizeOfDest.Height / 2 + length));
            }
            else if (DistBetweenSandD.Y == 0 && DistBetweenSandD.X > 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                EndOfStem.Y = MidOfSource.Y;
                EndOfStem.X = MidOfSource.X + (DistBetweenSandD.X - (SizeOfDest.Width / 2 + length));
            }
            else if (DistBetweenSandD.Y == 0 && DistBetweenSandD.X < 0)
            {
                DistBetweenSandD.Y = Math.Abs(DistBetweenSandD.Y);
                DistBetweenSandD.X = Math.Abs(DistBetweenSandD.X);

                EndOfStem.Y = MidOfSource.Y;
                EndOfStem.X = MidOfSource.X - (DistBetweenSandD.X - (SizeOfDest.Width / 2 + length));
            }
            else if (DistBetweenSandD.X == 0 && DistBetweenSandD.Y == 0)
            {
                EndOfStem.Y = MidOfDest.Y;
                EndOfStem.X = MidOfDest.X;
            }
            return (EndOfStem);
        }

        #endregion Private Helpers

        #region PublicEvents

        /// <summary>
        /// This pops up a message box to remove the sender if yes sender is removed
        /// </summary>
        /// <param name="sender">must be a ICommentViewModel</param>
        /// <param name="e">e</param>
        public void commentViewModel_Remove(object sender, EventArgs e)
        {
            if (MessageBox.Show("Delete this comment?", "Delete Comment", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                ICommentViewModel comment = sender as ICommentViewModel;
                comment.Moving -= new MouseButtonEventHandler(commentViewModel_StartMove);
                comment.Remove -= new EventHandler(commentViewModel_Remove);
                comment.Resizing -= new MouseButtonEventHandler(commentViewModel_StartResize);
                currentManipulation = Manipulation.Nothing;
                RequestRemoveHighLighting(comment, EventArgs.Empty);
                comments.Remove(comment);
                RemoveFromUIElement(comment);
            }
        }

        /// <summary>
        /// This sets everything up so the sender can be moved when MouseMove is called
        /// </summary>
        /// <param name="sender">must be a ICommentViewModel</param>
        /// <param name="e">e</param>
        public void commentViewModel_StartMove(object sender, MouseButtonEventArgs e)
        {
            if (isFreeFloating)
            {
                selectedComment = sender as ICommentViewModel;
                //focus so the richTextBox looses any selection
                selectedComment.GetView().Focus();

                //Mouse Capture so we get any events even if they are outside the View
                selectedComment.GetView().CaptureMouse();

                currentManipulation = Manipulation.Moving;
                Point mouseDownLocation = e.GetPosition(selectedComment.GetView());

                //While we are moving we will hide the line then when dropped we will put it back in
                RemoveLine(selectedComment);

                //This gets the offset from 0,0 the mouse picked the noteText up at
                initialOffset = new Point(
                    mouseDownLocation.X,
                    mouseDownLocation.Y);
            }
            else
            {
                selectedComment = sender as ICommentViewModel;
                currentManipulation = Manipulation.Nothing;
            }
        }

        /// <summary>
        /// This sets everything up so the sender can be Resized when MouseMove is called
        /// </summary>
        /// <param name="sender">must be a ICommentViewModel</param>
        /// <param name="e">e</param>
        public void commentViewModel_StartResize(object sender, MouseButtonEventArgs e)
        {
            selectedComment = sender as ICommentViewModel;

            Point mouseDownLocation = e.GetPosition(selectedComment.GetView());

            //this gives us the bottom right corner which is where mouse "should" be
            Point bottemRight = new Point(
                selectedComment.Location.X + selectedComment.Size.Width,
                selectedComment.Location.Y + selectedComment.Size.Height
                );

            //focus so the richTextBox looses any selection
            selectedComment.GetView().Focus();

            //Mouse Capture so we get any events even if they are outside the View
            selectedComment.GetView().CaptureMouse();

            currentManipulation = Manipulation.Resizing;

            //This gets the offset from bottom right corner the mouse picked the noteText up at
            initialOffset = new Point(
                (mouseDownLocation.X - bottemRight.X),
                (mouseDownLocation.Y - bottemRight.Y));
        }

        /// <summary>
        /// If moving this moves the sender else if resizing it resizes sender else nothing
        /// </summary>
        /// <param name="sender">must be a ICommentViewModel</param>
        /// <param name="e">e</param>
        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedComment == null)
            {
                currentManipulation = Manipulation.Nothing;
            }

            if (currentManipulation == Manipulation.Moving)
            {
                Point mouseDownLocation = e.GetPosition(selectedComment.GetView());

                //we subtract the offset to offset it so the noteText will still be relative to the mouse
                //when the mouse picked it up instead of the noteText jumping to make 0,0 at the mouse location
                Point newLocation = new Point(
                    mouseDownLocation.X - initialOffset.X,
                    mouseDownLocation.Y - initialOffset.Y
                    );

                setCommentLocation(selectedComment, newLocation);
            }
            else if (currentManipulation == Manipulation.Resizing)
            {
                Point mouseLocation = e.GetPosition(selectedComment.GetView());
                Point newBottemRight = new Point(
                    mouseLocation.X - initialOffset.X,
                    mouseLocation.Y - initialOffset.Y
                    );
                Point TopLeft = selectedComment.Location;
                Point newSize = new Point(newBottemRight.X - TopLeft.X, newBottemRight.Y - TopLeft.Y);
                if (newSize.X <= 22)
                {
                    newSize.X = 22;
                }
                if (newSize.Y <= 20)
                {
                    newSize.Y = 20;
                }
                selectedComment.Height = newSize.Y;
                selectedComment.Width = newSize.X;
            }
        }

        /// <summary>
        /// This clears moving or resizing
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e </param>
        public void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            currentManipulation = Manipulation.Nothing;

            //we will draw a newLine because something might have changed (moving it)
            DrawLine(selectedComment);
        }

        #endregion PublicEvents

        #region PrivateEvent

        /// <summary>
        /// This fires whenever a comment gets focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentViewModel_GotFocus(object sender, RoutedEventArgs e)
        {
            selectedComment = sender as ICommentViewModel;
            GotFocus(sender, e);
        }

        /// <summary>
        /// This is used to determine if the line associated with the commentViewModel needs to be
        /// dashed or solid
        /// </summary>
        /// <param name="cvm"></param>
        private void setLineProperties(ICommentViewModel cvm)
        {
            foreach (Line line in cvm.SnippetToCommentLine)
            {
                line.Stroke = cvm.LineBrush;
                line.StrokeThickness = (3);
            }
            if (selectedComment == cvm)
            {
                HighlightLine(cvm);
            }
            else
            {
                DashLine(cvm);
            }
        }

        /// <summary>
        /// This gets called when a comment looses focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentViewModel_LostFocus(object sender, RoutedEventArgs e)
        {
            ICommentViewModel cvm = sender as ICommentViewModel;
            setLineProperties(cvm);
            RequestOutlineHighLighting(cvm, EventArgs.Empty);

            if (cvm == selectedComment)
            {
                selectedComment = null;
            }
        }

        /// <summary>
        /// This gets called whenever a minimize is request and it replaces the current view with a
        /// collapsed view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentViewModel_Minimize(object sender, MouseButtonEventArgs e)
        {
            ICommentViewModel commentViewModel = (sender as ICommentViewModel);

            if (!isFreeFloating)
            {
                if (commentViewModel.UsingView)
                {
                    AbstractCommentView view = (commentViewModel).GetView();
                    StackPanel stackPanel = (view.Parent as StackPanel);

                    //get the index of the coallapsedView
                    int StackPanelIndex = stackPanel.Children.IndexOf(view);

                    stackPanel.Children.RemoveAt(StackPanelIndex);

                    stackPanel.Children.Insert(StackPanelIndex, (commentViewModel).GetCollapsedView());
                    (commentViewModel).UsingView = false;
                }
                else
                {
                    RemoveFromUIElement(commentViewModel);
                }
            }
            else
            {
                //note we do not remove it from the list because we are just "minimizing it"
                RemoveFromUIElement(commentViewModel);
            }
            DrawLine(commentViewModel);
        }

        /// <summary>
        /// This occurs when the command had mouse capture but then lost it and it sets the currentManipulation to nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommentManipulation_LostMouseCapture(object sender, MouseEventArgs e)
        {
            //we kill the Manipulation because we don't know where mouse is and we don't it the noteText to be
            //stuck to the mouse
            currentManipulation = Manipulation.Nothing;
        }

        #endregion PrivateEvent
    }
}