using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using ReviewInterfaceBase.View.Document;
using ReviewInterfaceBase.ViewModel.Comment.Location;

namespace ReviewInterfaceBase.ViewModel.Document.XpsDocument
{
    /// <summary>
    ///
    /// </summary>
    public class XpsDocumentViewModel : ISpatialDocumentViewModel
    {
        #region Delegates

        public event EventHandler ReviewItemSelectedChanged = delegate { };
        public event EventHandler ContentUpdated = delegate { };

        #endregion Delegates

        #region Fields

        private GlyphSelection glyphSelection = new GlyphSelection();
        private bool isDisplayed = false;
        private XpsDocumentView thisView = new XpsDocumentView();
        private bool reviewItemSelected = false;
        private List<Rectangle> selectedReviewItem;
        private int documentID;
        private RectangleSelectionHighlighting rectangleSelection;
        private StackPanel pagesHolder = new StackPanel();
        private int numberOfPages;

        #endregion Fields

        #region Properties

        public List<StackPanel> Children
        {
            get
            {
                List<StackPanel> list = new List<StackPanel>();
                list.Add(new StackPanel());
                return list;
            }
        }

        public List<RowDefinition> Lines
        {
            get
            {
                List<RowDefinition> list = new List<RowDefinition>();
                int i = 0;
                while (i < numberOfPages)
                {
                    list.Add(new RowDefinition());
                    i++;
                }
                return list;
            }
        }

        public bool IsDisplayed
        {
            get
            {
                return isDisplayed;
            }
            set
            {
                isDisplayed = value;
            }
        }

        public bool ReviewItemSelected
        {
            get
            {
                return reviewItemSelected;
            }
            set
            {
                if (value == false)
                {
                    rectangleSelection.ClearSelection();
                    reviewItemSelected = value;
                }
                else
                {
                    throw new Exception("Can only set ReviewItemSelected to false");
                }
            }
        }

        #endregion Properties

        #region Constructor

        public XpsDocumentViewModel(int documentID, StreamReader streamReader, string fileExtension)
        {
            this.documentID = documentID;
            if (fileExtension == ".xps")
            {
                XpsConverter xpsConverter = new XpsConverter();
                xpsConverter.GetPageLocations(streamReader.BaseStream);

                getAllPages(xpsConverter);

                pagesHolder.Background = new SolidColorBrush(Colors.Gray);

                thisView.LayoutRoot.Children.Add(pagesHolder);

                numberOfPages = xpsConverter.NumberOfPages;

                pagesHolder.SizeChanged += new SizeChangedEventHandler(pagesHolder_SizeChanged);

                rectangleSelection = new RectangleSelectionHighlighting(thisView.CommentReferenceCanvas, thisView.HitCanvas, glyphSelection);
                rectangleSelection.SelectionChanged += new EventHandler(xpsSelection_SelectionChanged);

                ContentUpdated(this, EventArgs.Empty);
            }
        }

        #endregion Constructor

        #region Private Methods

        private void getAllPages(XpsConverter xpsConverter)
        {
            int currentPage = 0;

            //remove the old listener
            glyphSelection.SelectionChanged -= new EventHandler(selection_SelectionChanged);

            //Create a new GlyphSlection
            glyphSelection = new GlyphSelection();
            glyphSelection.SelectionChanged += new EventHandler(selection_SelectionChanged);
            while (currentPage < xpsConverter.NumberOfPages)
            {
                Canvas xpsPage = xpsConverter.GetPage(currentPage);

                //List<Panel> panels = getAllPanels(xpsPage);
                glyphSelection.initilizeGlyph(xpsPage);

                xpsPage.Background = new SolidColorBrush(Colors.White);
                xpsPage.Margin = new Thickness(0, 0, 0, 10);
                pagesHolder.Children.Add(xpsPage);
                currentPage++;
            }
        }

        private void selection_SelectionChanged(object sender, EventArgs e)
        {
            reviewItemSelected = true;
            selectedReviewItem = (sender as GlyphSelection).Selection;
            ReviewItemSelectedChanged(this, EventArgs.Empty);
        }

        private void pagesHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size pagesHolderSize = pagesHolder.RenderSize;

            thisView.CommentReferenceCanvas.Width = pagesHolderSize.Width;
            thisView.CommentReferenceCanvas.Height = pagesHolderSize.Height;
        }

        /*void zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScaleTransform scalexForm= new ScaleTransform();
            st.CenterX = 0;
            st.CenterY = 0;
            st.ScaleX = e.NewValue;
            st.ScaleY = e.NewValue;
            xpsCanvas.RenderTransform = scalexForm;
        }*/

        private void xpsSelection_SelectionChanged(object sender, EventArgs e)
        {
            List<Rectangle> rect = new List<Rectangle>();
            rect.Add(rectangleSelection.Selection);
            selectedReviewItem = rect;
            if (selectedReviewItem != null)
            {
                reviewItemSelected = true;
            }
            else
            {
                reviewItemSelected = false;
            }
            ReviewItemSelectedChanged(this, EventArgs.Empty);
        }

        #endregion Private Methods

        #region Public Methods

        public ISpatialLocation GetReferenceLocationFromXml(XElement xmlLocation)
        {
            List<Rectangle> rectangles = new List<Rectangle>();

            int pageNumber = Int32.Parse(xmlLocation.Attribute("PageNumber").Value);

            foreach (XElement xmlRectangle in xmlLocation.Descendants("Rectangle"))
            {
                XElement xmlTopLeft = xmlRectangle.Descendants("TopLeft").ElementAt(0);
                XElement xmlSize = xmlRectangle.Descendants("Size").ElementAt(0);

                Rectangle rect = new Rectangle();

                rect.SetValue(Canvas.LeftProperty, double.Parse(xmlTopLeft.Attribute("Left").Value));
                rect.SetValue(Canvas.TopProperty, double.Parse(xmlTopLeft.Attribute("Top").Value));

                rect.Width = double.Parse(xmlSize.Attribute("Width").Value);
                rect.Height = double.Parse(xmlSize.Attribute("Height").Value);

                rectangles.Add(rect);
            }

            XpsLocation xpsLocation = new XpsLocation(rectangles, pageNumber);

            return xpsLocation;
        }

        public IDocumentView GetView()
        {
            return thisView;
        }

        public Size GetContentSize()
        {
            return new Size(thisView.LayoutRoot.ActualWidth, thisView.LayoutRoot.ActualHeight);
        }

        public ISpatialLocation GetReferenceLocation()
        {
            if (reviewItemSelected == true)
            {
                int pageNumber = -1;

                pageNumber = pagesHolder.Children.IndexOf((selectedReviewItem[0].Parent as Canvas).Parent as UIElement);

                return new XpsLocation(selectedReviewItem, pageNumber);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<FrameworkElement> CreateReferenceLocationHighlighting(ILocation referenceLocation, Brush highlightColor)
        {
            rectangleSelection.ClearSelection();
            glyphSelection.ClearSelection();

            List<Rectangle> rectangles = (referenceLocation as XpsLocation).Rectangles;

            List<FrameworkElement> frameworkElements = new List<FrameworkElement>();

            frameworkElements.AddRange(from c in rectangles where c is FrameworkElement select c as FrameworkElement);

            foreach (FrameworkElement fe in frameworkElements)
            {
                if (fe.Parent == null)
                {
                    XpsLocation location = (referenceLocation as XpsLocation);

                    if (location.PageNumber == -1)
                    {
                        thisView.CommentReferenceCanvas.Children.Add(fe);
                    }
                    else
                    {
                        (pagesHolder.Children[location.PageNumber] as Canvas).Children.Add(fe);
                    }
                }
            }

            return (frameworkElements);
        }

        public void RemoveReferenceLocationHighlighting(IEnumerable<FrameworkElement> toBeRemoved)
        {
            //again this should contain one rectangle but just in case we changed to contain many this will still work
            foreach (FrameworkElement fe in toBeRemoved)
            {
                rectangleSelection.XpsCanvas.Children.Remove(fe);
            }
        }

        public void SetReferenceLocationHighlightingToFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush highlightColor)
        {
            foreach (FrameworkElement fe in toBeHighlighted)
            {
                (fe as Rectangle).StrokeThickness = 0;
                (fe as Rectangle).Fill = highlightColor;
                (fe as Rectangle).Stroke = new SolidColorBrush(Colors.Transparent);
            }
        }

        public void SetReferenceLocationHighlightingToNotFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush outlineColor)
        {
            foreach (FrameworkElement fe in toBeHighlighted)
            {
                (fe as Rectangle).StrokeThickness = 2;
                (fe as Rectangle).Stroke = outlineColor;
                (fe as Rectangle).Fill = new SolidColorBrush(Colors.Transparent);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", documentID.ToString());
        }

        #endregion Public Methods

        #region IDocumentViewModel Members

        public int DocumentID
        {
            get { return documentID; }
        }

        public object FindNext(object lastFound, FindWindow.FindWindowOptions options)
        {
            //NEED TO IMPLIMENT THIS
            return null;
        }

        #endregion IDocumentViewModel Members
    }
}