using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View.DocumentHolder;
using ReviewInterfaceBase.ViewModel.Comment;
using ReviewInterfaceBase.ViewModel.Comment.Location;
using ReviewInterfaceBase.ViewModel.Document;
using ReviewInterfaceBase.ViewModel.Document.ChemProVDocument;
using ReviewInterfaceBase.ViewModel.Document.TextFileDocument;
using ReviewInterfaceBase.ViewModel.Document.XpsDocument;

namespace ReviewInterfaceBase.ViewModel.DocumentHolder
{
    /// <summary>
    /// This is the holds all the spatial documents
    /// </summary>
    public class SpatialDocumentHolderViewModel : IDocumentHolderViewModel
    {
        #region Delegetes

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event EventHandler FindWindowRequested = delegate { };

        #endregion Delegetes

        #region Fields

        private bool allowNewComments = false;

        /// <summary>
        /// This timer just helps us to redraw the lines we want to make sure we are doing it pretty often so everything gets updated
        /// </summary>
        private DispatcherTimer updateTimer = new DispatcherTimer();

        /// <summary>
        /// This is our reference to the commentManipulation which will take care of almost all the code to control comments
        /// </summary>
        private CommentManipulation commentManipulation;

        /// <summary>
        /// This is a flag so we know if we are dragging the Grid Splitter again thinking about making this its own class.
        /// </summary>
        private bool isDraggingGridSplitter = false;

        /// <summary>
        /// This is the view that this ViewModel is manipulating
        /// </summary>
        private SpatialDocumentHolderView thisView;

        /// <summary>
        /// This is a reference to the DocumentViewModel that inherits from ISpatialDocumentViewModel we are using
        /// </summary>
        private ISpatialDocumentViewModel spatialDocumentViewModel;

        private Button addNewCommentButton;

        private bool scrollBarsAreAttached = false;

        #endregion Fields

        #region Properties

        public bool IsDisplayed
        {
            get { return spatialDocumentViewModel.IsDisplayed; }
            set
            {
                spatialDocumentViewModel.IsDisplayed = value;
                if (value)
                {
                    if (!scrollBarsAreAttached)
                    {
                        thisView.LayoutUpdated += new EventHandler(thisView_LayoutUpdated);
                    }
                    updateTimer.Start();
                }
                else if (value)
                {
                    updateTimer.Stop();
                }
                PropertyChanged(this, new PropertyChangedEventArgs("IsDisplayed"));
            }
        }

        public int idOfDocument
        {
            get
            {
                return spatialDocumentViewModel.DocumentID;
            }
        }

        #endregion Properties

        #region Constructor

        public SpatialDocumentHolderViewModel(int documentID, StreamReader streamReader, string fileExtension)
        {
            thisView = new SpatialDocumentHolderView(this);

            //This may need to go into a factory but with only one if a factory might be overkill
            if (fileExtension == ".cpml")
            {
                spatialDocumentViewModel = new ChemProVDocumentViewModel(documentID, streamReader, fileExtension);
            }
            else if (fileExtension == ".xps")
            {
                spatialDocumentViewModel = new XpsDocumentViewModel(documentID, streamReader, fileExtension);
            }
            else
            {
                spatialDocumentViewModel = new TextDocumentViewModel(documentID, streamReader, fileExtension);
            }

            spatialDocumentViewModel.ReviewItemSelectedChanged += new EventHandler(spatialDocumentViewModel_ReviewItemSelectedChanged);
            spatialDocumentViewModel.ContentUpdated += new EventHandler(spatialDocumentViewModel_ContentUpdated);

            thisView.DocumentHolder.Children.Add(spatialDocumentViewModel.GetView() as UIElement);

            //We use this to set up our scrollbar hacks
            thisView.LayoutUpdated += new EventHandler(thisView_LayoutUpdated);

            //toolbar button events
            thisView.ToggleViewButton.Click += new RoutedEventHandler(ToggleViewButton_Click);
            thisView.ShowAllCommentButton.Click += new RoutedEventHandler(ShowAllCommentsButton_Click);
            thisView.HideAllCommentsButton.Click += new RoutedEventHandler(HideAllCommentsButton_Click);
            thisView.SearchButton.Click += new RoutedEventHandler(SearchButton_Click);
            //thisView.SearchTextBox.TextChanged += new TextChangedEventHandler(SearchTextBox_TextChanged);

            //LayoutRoot events
            thisView.MainGrid.MouseMove += new MouseEventHandler(LayoutRoot_MouseMove);
            thisView.MainGrid.MouseLeftButtonUp += new MouseButtonEventHandler(LayoutRoot_MouseLeftButtonUp);

            //gridSpliter events

            thisView.MainGridSplitter.MouseLeftButtonDown += new MouseButtonEventHandler(MainGridSplitter_MouseLeftButtonDown);
            thisView.MainGridSplitter.MouseMove += new MouseEventHandler(MainGridSplitter_MouseMove);
            thisView.MainGridSplitter.MouseLeftButtonUp += new MouseButtonEventHandler(MainGridSplitter_MouseLeftButtonUp);
            thisView.MainGridSplitter.LostMouseCapture += new MouseEventHandler(MainGridSplitter_LostMouseCapture);

            //Document viewer events
            thisView.DocumentHolder.MouseRightButtonDown += new MouseButtonEventHandler(DocumentHolder_MouseRightButtonDown);
            thisView.DocumentHolder.MouseWheel += new MouseWheelEventHandler(DocumentHolder_MouseWheel);
            //thisView.DocumentHolder.LostFocus += new RoutedEventHandler(DocumentHolder_LostFocus);
            //thisView.DocumentHolder.GotFocus += new RoutedEventHandler(DocumentHolder_GotFocus);

            thisView.CommentStackPanelHolder.MouseWheel += new MouseWheelEventHandler(CommentHolderScrollViewer_MouseWheel);

            //commentManipulation = new CommentManipulation(thisView.CommentCanvasOverlay,
            //  thisView.CommentCanvasRightSideOverlay, thisView.CommentStackPanelHolder);

            commentManipulation = new CommentManipulation(spatialDocumentViewModel.DocumentID, this, thisView.CommentCanvasOverlay, thisView.CommentStackPanelHolder,
                thisView.LineCanvasOverlayLeftSide, thisView.LineCanvasOverlayMiddle, thisView.LineCanvasOverlayRightSide);

            //listen for the CommentManipulation Events
            commentManipulation.RequestSolidHighLighting += new EventHandler(commentManipulation_RequestSolidHighLighting);
            commentManipulation.RequestOutlineHighLighting += new EventHandler(commentManipulation_RequestOutlineHighLighting);
            commentManipulation.RequestRemoveHighLighting += new EventHandler(commentManipulation_RequestRemoveHighLighting);
            commentManipulation.GotFocus += new EventHandler(commentManipulation_GotFocus);

            spatialDocumentViewModel_ContentUpdated();

            //This will start once we are being displayed and stop when are not
            //this ticks every 20 milliseconds to update the lines
            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            updateTimer.Tick += new EventHandler(updateTimer_Tick);
        }

        #endregion Constructor

        #region Public Methods

        public void AllowNewComments()
        {
            if (allowNewComments == false)
            {
                allowNewComments = true;
                addNewCommentButton = new Button()
                {
                    Content = new Image() { Source = new BitmapImage(new Uri("/Osble;component/View/Icons/NoteHS.png", UriKind.Relative)) },
                    Margin = new Thickness(0, 0, 5, 0)
                };

                ToolTipService.SetToolTip(addNewCommentButton, "To add a comment, select a snippet of the document and either right-click or click this button.");

                addNewCommentButton.Click += new RoutedEventHandler(AddNewCommentButton_Click);
                addNewCommentButton.IsEnabled = false;

                thisView.ButtonToolbar.Children.Add(addNewCommentButton);
            }
        }

        /// <summary>
        /// This searches the document first for a match then if none is found it searches the comments
        /// </summary>
        /// <param name="lastFound">this takes the object that was found by this SpatialDocumentHolderViewModel last time</param>
        /// <returns>a new object that matches the search string</returns>
        public object FindNext(object foundLast, ReviewInterfaceBase.ViewModel.FindWindow.FindWindowOptions options)
        {
            object found = foundLast;
            if (foundLast == null || !(foundLast is ICommentViewModel))
            {
                found = spatialDocumentViewModel.FindNext(foundLast, options);
            }

            //cant do else as if lastFound is set again the if statement
            if (found == null || foundLast is ICommentViewModel)
            {
                //so if lastFound was a ICommentViewModel then it will type cast it to be so if it aint it will
                //type cast it to null which is what found must have been equal too. Tricky huh?
                found = commentManipulation.FindNextComment(foundLast as ICommentViewModel, options.LookingFor);
            }
            return found;
        }

        public void ScrollToComment(object obj)
        {
            if (obj as ICommentViewModel != null)
            {
                ICommentViewModel cvm = obj as ICommentViewModel;

                if (commentManipulation.IsFreeFloating)
                {
                    GeneralTransform gf = cvm.GetView().TransformToVisual(thisView.DocumentViewerScrollViewer);
                    Point point = gf.Transform(new Point(0, 0));

                    thisView.DocumentViewerScrollViewer.ScrollToVerticalOffset(point.Y);
                    thisView.DocumentViewerScrollViewer.ScrollToHorizontalOffset(point.X);
                }
                else
                {
                }
            }
            else if (obj as Run != null)
            {
                Run run = obj as Run;

                if (commentManipulation.IsFreeFloating)
                {
                    Rect rect = run.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                    Point point = new Point(rect.Left, rect.Top);
                    thisView.DocumentViewerScrollViewer.ScrollToVerticalOffset(point.Y);
                    thisView.DocumentViewerScrollViewer.ScrollToHorizontalOffset(point.X);
                }
                else
                {
                }
            }
        }

        public IDocumentViewModel GetDocumentViewModel()
        {
            return spatialDocumentViewModel;
        }

        #endregion Public Methods

        #region DocumentView Event Handlers

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            //this will fail if the comment or the reference is no longer being displayed
            commentManipulation.UpdateLines();
        }

        private void spatialDocumentViewModel_ReviewItemSelectedChanged(object sender, EventArgs e)
        {
            if (addNewCommentButton != null)
            {
                if (spatialDocumentViewModel.ReviewItemSelected == true)
                {
                    addNewCommentButton.IsEnabled = true;
                }
                else
                {
                    addNewCommentButton.IsEnabled = false;
                }
            }
        }

        private void spatialDocumentViewModel_ContentUpdated(object sender, EventArgs e)
        {
            spatialDocumentViewModel_ContentUpdated();
        }

        private void spatialDocumentViewModel_ContentUpdated()
        {
            thisView.CommentStackPanelHolder.RowDefinitions.Clear();
            thisView.CommentStackPanelHolder.Children.Clear();

            foreach (RowDefinition rd in spatialDocumentViewModel.Lines)
            {
                thisView.CommentStackPanelHolder.RowDefinitions.Add(rd);
            }

            foreach (StackPanel sp in spatialDocumentViewModel.Children)
            {
                thisView.CommentStackPanelHolder.Children.Add(sp);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FindWindowRequested(this, EventArgs.Empty);
        }

        private void commentManipulation_GotFocus(object sender, EventArgs e)
        {
            /*
            if (currentSelection != null)
            {
                selectionHighlighting.ClearHighlighting(currentSelection);
            }
             */
        }

        private void DocumentHorizontalScrollViewerHorizontalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            thisView.DocumentViewerScrollViewer.ScrollToHorizontalOffset(e.NewValue);
            commentManipulation.UpdateLines();
        }

        private void AddNewCommentButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewComment();
        }

        private void CommentHolderScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //this only fires if the mouse is over a comment if it aint then it don't fire this leads to unexpected behaviors
            //so until i find a hack to make it always work this is not possible
            //this stops the comments from scrolling via the mouse wheel and makes the VerticalScrollBar scroll instead
            e.Handled = true;

            //MainVerticalScrollBarMouseWheel(e.Delta);
        }

        private void DocumentHolder_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //this stops the document from scrolling via the mouse wheel and makes the VerticalScrollBar scroll instead
            e.Handled = true;
            //commentManipulation.HideLines();
            MainVerticalScrollBarMouseWheel(e.Delta);
        }

        private void thisView_LayoutUpdated(object sender, EventArgs e)
        {
            thisView.LayoutUpdated -= new EventHandler(thisView_LayoutUpdated);

            ScrollBar scrollBar;

            //it appears if an element is loaded but not shown layoutUpdate is fired even though it hasn't been
            //if this is the case the try will fail and we will attach the handler back on and try again later
            //this could be problematic if it never works
            try
            {
                scrollBar = attachScrollViewsTogether(thisView.DocumentViewerScrollViewer, thisView.DocumentHorizontalScrollViewer, Orientation.Horizontal);
                scrollBar.Scroll += new ScrollEventHandler(DocumentHorizontalScrollViewerHorizontalScrollBar_Scroll);

                scrollBar = attachScrollViewsTogether(thisView.CommentHolderScrollViewer, thisView.CommentStackHorizontalScrollBar, Orientation.Horizontal);
                scrollBar.Scroll += new ScrollEventHandler(CommentHolderScrollViewerHorizontalScrollBar_Scroll);

                scrollBar = attachScrollViewsTogether(thisView.CommentHolderScrollViewer, thisView.MainVerticalScrollBar, Orientation.Vertical);
                scrollBar.Scroll += new ScrollEventHandler(CommentHolderScrollViewerHorizontalScrollBar_Scroll);

                scrollBar = attachScrollViewsTogether(thisView.DocumentViewerScrollViewer, thisView.MainVerticalScrollBar, Orientation.Vertical);
                scrollBar.Scroll += new ScrollEventHandler(DocumentViewerScrollViewerVerticalScrollBar_Scroll);

                thisView.DocumentHolder.SizeChanged += new SizeChangedEventHandler(DocumentViewerScrollViewer_SizeChanged);
                thisView.CommentStackPanelHolder.SizeChanged += new SizeChangedEventHandler(CommentStackPanelHolder_SizeChanged);
                scrollBarsAreAttached = true;
            }
            catch
            {
                //we will try again later
                thisView.LayoutUpdated += new EventHandler(thisView_LayoutUpdated);
            }
        }

        private void CommentStackPanelHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //attach the scrollbar to the longer item
            if (thisView.CommentStackPanelHolder.ActualHeight < thisView.DocumentHolder.ActualHeight)
            {
                attachScrollViewsTogether(thisView.DocumentViewerScrollViewer, thisView.MainVerticalScrollBar, Orientation.Vertical);
            }
            else
            {
                attachScrollViewsTogether(thisView.CommentHolderScrollViewer, thisView.MainVerticalScrollBar, Orientation.Vertical);
            }
            commentManipulation.UpdateLines();
        }

        private void DocumentViewerScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //attach the scrollbar to the longer item
            if (thisView.CommentStackPanelHolder.ActualHeight < thisView.DocumentHolder.ActualHeight)
            {
                attachScrollViewsTogether(thisView.DocumentViewerScrollViewer, thisView.MainVerticalScrollBar, Orientation.Vertical);
            }
            else
            {
                attachScrollViewsTogether(thisView.CommentHolderScrollViewer, thisView.MainVerticalScrollBar, Orientation.Vertical);
            }
            commentManipulation.UpdateLines();
        }

        private void LayoutRoot_MouseMove(object sender, MouseEventArgs e)
        {
            commentManipulation.MouseMove(sender, e);
        }

        /// <summary>
        /// Not to be called except by TextFileDocumentView
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            commentManipulation.MouseLeftButtonUp(sender, e);
        }

        private void CommentHolderScrollViewerHorizontalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            thisView.CommentHolderScrollViewer.ScrollToHorizontalOffset(e.NewValue);
            commentManipulation.UpdateLines();
        }

        private void DocumentViewerScrollViewerVerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            thisView.DocumentViewerScrollViewer.ScrollToVerticalOffset(e.NewValue);
            thisView.CommentHolderScrollViewer.ScrollToVerticalOffset(e.NewValue);
            if (e.ScrollEventType == ScrollEventType.EndScroll)
            {
                commentManipulation.UpdateLines();
            }
            //ScrollCommentViewer();
        }

        private void MainGridSplitter_LostMouseCapture(object sender, MouseEventArgs e)
        {
            isDraggingGridSplitter = false;
        }

        private void MainGridSplitter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingGridSplitter = false;
            (sender as GridSplitter).ReleaseMouseCapture();
            commentManipulation.UpdateLines();
            thisView.UpdateLayout();
        }

        private void MainGridSplitter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingGridSplitter = true;
            (sender as GridSplitter).CaptureMouse();
        }

        private void MainGridSplitter_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingGridSplitter)
            {
                thisView.MainGrid.ColumnDefinitions[1].Width = new GridLength(5);
                thisView.MainGrid.ColumnDefinitions[2].Width = new GridLength(thisView.ActualWidth - e.GetPosition(thisView).X);
                //thisView.MainGrid.ColumnDefinitions[0].Width = new GridLength(thisView.ActualWidth - (thisView.MainGrid.ColumnDefinitions[1].ActualWidth + thisView.MainGrid.ColumnDefinitions[2].ActualWidth));
                commentManipulation.UpdateLines();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //commentManipulation.DisplayOnlyCommentsWithText(thisView.SearchTextBox.Text, false);
        }

        private void MenuItemAddComment_Click(object sender, RoutedEventArgs e)
        {
            AddNewComment();
        }

        private void DocumentHolder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //If we allow comments then make Context Menu because at the time of writing this had one item that was to add comment
            if (allowNewComments)
            {
                //This creates a new contextMenu
                ContextMenu cm = new ContextMenu();
                string[] options = new string[] { "add comment" };
                foreach (string s in options)
                {
                    MenuItem mi = new MenuItem();
                    mi.Header = s;
                    cm.Items.Add(mi);
                    mi.Click += new RoutedEventHandler(MenuItemAddComment_Click);
                }

                cm.HorizontalOffset = e.GetPosition(cm.Parent as UIElement).X;
                cm.VerticalOffset = e.GetPosition(cm.Parent as UIElement).Y;
                cm.IsOpen = true;
            }
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            commentManipulation.ToggleView();
        }

        private void ShowAllCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            commentManipulation.ShowAll();
        }

        private void HideAllCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            commentManipulation.HideAll();
        }

        #endregion DocumentView Event Handlers

        #region NoteText Manipulation Event Handlers

        private void commentManipulation_RequestOutlineHighLighting(object sender, EventArgs e)
        {
            ICommentViewModel commentViewModel = sender as ICommentViewModel;
            ICommentViewModel cvm = sender as ICommentViewModel;
            if (cvm.SnippetHighlighting == null)
            {
                cvm.SnippetHighlighting = new List<FrameworkElement>(spatialDocumentViewModel.CreateReferenceLocationHighlighting(cvm.referenceLocation, cvm.TextBrush));
            }
            spatialDocumentViewModel.SetReferenceLocationHighlightingToNotFocused(cvm.SnippetHighlighting, cvm.TextBrush);
        }

        private void commentManipulation_RequestRemoveHighLighting(object sender, EventArgs e)
        {
            spatialDocumentViewModel.RemoveReferenceLocationHighlighting((sender as ICommentViewModel).SnippetHighlighting);
            (sender as ICommentViewModel).SnippetHighlighting = null;
        }

        private void commentManipulation_RequestSolidHighLighting(object sender, EventArgs e)
        {
            ICommentViewModel cvm = sender as ICommentViewModel;
            if (cvm.SnippetHighlighting == null)
            {
                cvm.SnippetHighlighting = new List<FrameworkElement>(spatialDocumentViewModel.CreateReferenceLocationHighlighting(cvm.referenceLocation, cvm.TextBrush));
            }
            spatialDocumentViewModel.SetReferenceLocationHighlightingToFocused(cvm.SnippetHighlighting, cvm.TextBrush);
        }

        #endregion NoteText Manipulation Event Handlers

        #region Private Methods

        private void AddNewComment()
        {
            if (spatialDocumentViewModel.ReviewItemSelected != false)
            {
                ISpatialLocation location = spatialDocumentViewModel.GetReferenceLocation();
                if (location != null)
                {
                    commentManipulation.addNewPeerReviewComment(location, spatialDocumentViewModel.GetContentSize());
                    spatialDocumentViewModel.ReviewItemSelected = false;
                    return;
                }
            }
            MessageBox.Show("You must select a reviewable item before you can make a comment");
        }

        private void MainVerticalScrollBarMouseWheel(double mouseDelta)
        {
            ScrollBar mainSC = thisView.MainVerticalScrollBar.Descendents().OfType<ScrollBar>().LastOrDefault(elem => elem.Name == "VerticalScrollBar");
            mainSC.Value += mouseDelta * -1;
            thisView.DocumentViewerScrollViewer.ScrollToVerticalOffset(mainSC.Value);
            thisView.CommentHolderScrollViewer.ScrollToVerticalOffset(thisView.CommentHolderScrollViewer.VerticalOffset + mouseDelta * -1);

            //commentManipulation.HideLines();
        }

        private ScrollBar attachScrollViewsTogether(ScrollViewer controlee, ScrollViewer controler, Orientation orientation)
        {
            //real being the one attached to the object hence the controlee
            ScrollBar realScrollBar;

            //the fake one being the one we are using to control the controlee one
            ScrollBar fakeScrollBar;
            if (orientation == Orientation.Horizontal)
            {
                realScrollBar = controlee.Descendents().OfType<ScrollBar>().LastOrDefault(elem => elem.Name == "HorizontalScrollBar");
                fakeScrollBar = controler.Descendents().OfType<ScrollBar>().FirstOrDefault(elem => elem.Name == "HorizontalScrollBar");
            }
            else
            {
                realScrollBar = controlee.Descendents().OfType<ScrollBar>().LastOrDefault(elem => elem.Name == "VerticalScrollBar");
                fakeScrollBar = controler.Descendents().OfType<ScrollBar>().FirstOrDefault(elem => elem.Name == "VerticalScrollBar");
            }

            //if for any reason either are null we return throw an exception so the function who calls us know we didn't do anything
            if (realScrollBar == null || fakeScrollBar == null)
            {
                throw new Exception("Could not get inner scrollbars");
            }

            Binding maximun = new Binding("Maximum") { Source = realScrollBar };
            Binding Minimum = new Binding("Minimum") { Source = realScrollBar };
            Binding SmallChange = new Binding("SmallChange") { Source = realScrollBar };
            Binding LargeChange = new Binding("LargeChange") { Source = realScrollBar };
            Binding viewportSize = new Binding("ViewportSize") { Source = realScrollBar };

            fakeScrollBar.SetBinding(ScrollBar.MaximumProperty, maximun);
            fakeScrollBar.SetBinding(ScrollBar.MinimumProperty, Minimum);
            fakeScrollBar.SetBinding(ScrollBar.SmallChangeProperty, SmallChange);
            fakeScrollBar.SetBinding(ScrollBar.LargeChangeProperty, LargeChange);
            fakeScrollBar.SetBinding(ScrollBar.ViewportSizeProperty, viewportSize);

            //these appear to be hard-code values in a real scroll viewer and when they are set the NotifyProperty is not
            //thrown and thus binding them will not work
            fakeScrollBar.LargeChange = 500;
            fakeScrollBar.SmallChange = 15;

            return fakeScrollBar;
        }

        private void ScrollCommentViewer()
        {
            //first we find the top most pixel of the DocumentViewer that is currently being displayed
            ScrollBar scrollbar = thisView.MainVerticalScrollBar.Descendents().OfType<ScrollBar>().FirstOrDefault(elem => elem.Name == "VerticalScrollBar");

            double percentOfHeight = scrollbar.Value / scrollbar.ViewportSize;

            double height = thisView.DocumentHolder.ActualHeight;

            double TopOfDisplay = percentOfHeight * height;

            double BottemOfDisplay = TopOfDisplay + thisView.DocumentViewerScrollViewer.ActualHeight;

            ICommentViewModel HeighestCVM = null;

            //Then we go through our list and find the noteText with highest most associated text
            foreach (ICommentViewModel cvm in commentManipulation.Comments)
            {
                if ((double)cvm.SnippetHighlighting[0].GetValue(Canvas.TopProperty) > TopOfDisplay && (double)cvm.SnippetHighlighting[0].GetValue(Canvas.TopProperty) < BottemOfDisplay)
                {
                    if (HeighestCVM == null)
                    {
                        HeighestCVM = cvm;
                    }
                    else
                    {
                        if ((double)HeighestCVM.SnippetHighlighting[0].GetValue(Canvas.TopProperty) <
                            (double)cvm.SnippetHighlighting[0].GetValue(Canvas.TopProperty))
                        {
                            HeighestCVM = cvm;
                        }
                    }
                }
            }

            //now we find the position the CommentHolderScrollBar should be at in order to display that noteText at the top

            if (HeighestCVM != null)
            {
                GeneralTransform objGeneralTransform = HeighestCVM.GetView().TransformToVisual(Application.Current.RootVisual as UIElement);
                Point point = objGeneralTransform.Transform(new Point(0, 0));
                double HeighestCommentViewTop = point.Y;

                double percentage = HeighestCommentViewTop / height;

                scrollbar = thisView.CommentStackPanelHolder.Descendents().OfType<ScrollBar>().FirstOrDefault(elem => elem.Name == "HorizontalScrollBar");

                thisView.CommentHolderScrollViewer.ScrollToVerticalOffset(percentage * scrollbar.ViewportSize);
            }
        }

        #endregion Private Methods

        #region IDocumentViewModel Members

        public void LoadIssueVotingComments(XElement xmlDocument, NoteAuthor author)
        {
            foreach (XElement xmlComment in xmlDocument.Descendants("Comments").ElementAt(0).Descendants("Comment"))
            {
                ISpatialLocation location = spatialDocumentViewModel.GetReferenceLocationFromXml(xmlComment.Descendants("Location").First());
                commentManipulation.addIssueVotingComment(xmlComment, author, location, spatialDocumentViewModel.GetContentSize());
            }
        }

        public void LoadIssueVotingCommentsWithoutIssueVoting(XElement xmlDocument, NoteAuthor author)
        {
            foreach (XElement xmlComment in xmlDocument.Descendants("Comments").ElementAt(0).Descendants("Comment"))
            {
                ISpatialLocation location = spatialDocumentViewModel.GetReferenceLocationFromXml(xmlComment.Descendants("Location").First());
                commentManipulation.addIssueVotingCommentWithoutIssueVoting(xmlComment, author, location, spatialDocumentViewModel.GetContentSize());
            }
        }

        public IDocumentHolderView GetView()
        {
            return thisView;
        }

        public FrameworkElement GetContent()
        {
            return thisView.DocumentHolder;
        }

        public FrameworkElement GetContentScrollViewer()
        {
            return thisView.DocumentViewerScrollViewer;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Document");
            spatialDocumentViewModel.WriteXml(writer);
            writer.WriteStartElement("Comments");
            commentManipulation.WriteXml(writer);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        #endregion IDocumentViewModel Members
    }
}