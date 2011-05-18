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
    public class XpsDocumentViewModel : ISpatialDocumentViewModel
    {
        public event EventHandler ReviewItemSelectedChanged = delegate { };
        public event EventHandler ContentUpdated = delegate { };

        private bool isDisplayed = false;
        private XpsDocumentView thisView = new XpsDocumentView();
        private bool reviewItemSelected = false;
        private Rectangle selectedReviewItem;
        private int documentID;
        private XpsSelectionHighlighting xpsSelection;
        private StackPanel pagesHolder = new StackPanel();
        private int numberOfPages;

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

        private void getAllPages(XpsConverter xpsConverter)
        {
            int currentPage = 0;
            while (currentPage < xpsConverter.NumberOfPages)
            {
                Canvas xpsPage = xpsConverter.GetPage(currentPage);
                xpsPage.Background = new SolidColorBrush(Colors.White);
                xpsPage.Margin = new Thickness(0, 0, 0, 10);
                pagesHolder.Children.Add(xpsPage);
                currentPage++;
            }
        }

        public XpsDocumentViewModel(int documentID, StreamReader streamReader, string fileExtension)
        {
            this.documentID = documentID;
            if (fileExtension == ".xps")
            {
                XpsConverter xpsConverter = new XpsConverter();
                xpsConverter.GetPageLocations(streamReader.BaseStream);

                getAllPages(xpsConverter);

                pagesHolder.Background = new SolidColorBrush(Colors.LightGray);

                thisView.LayoutRoot.Children.Add(pagesHolder);

                numberOfPages = xpsConverter.NumberOfPages;

                pagesHolder.SizeChanged += new SizeChangedEventHandler(pagesHolder_SizeChanged);

                xpsSelection = new XpsSelectionHighlighting(thisView.CommentReferenceCanvas);

                ContentUpdated(this, EventArgs.Empty);
            }
        }

        private void pagesHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size pagesHolderSize = pagesHolder.RenderSize;

            thisView.CommentReferenceCanvas.Width = pagesHolderSize.Width;
            thisView.CommentReferenceCanvas.Height = pagesHolderSize.Height;

            xpsSelection = new XpsSelectionHighlighting(thisView.CommentReferenceCanvas);

            xpsSelection.SelectionChanged += new EventHandler(xpsSelection_SelectionChanged);
        }

        /*void zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScaleTransform st = new ScaleTransform();
            st.CenterX = 0;
            st.CenterY = 0;
            st.ScaleX = e.NewValue;
            st.ScaleY = e.NewValue;
            xpsCanvas.RenderTransform = st;
        }*/

        private void xpsSelection_SelectionChanged(object sender, EventArgs e)
        {
            selectedReviewItem = xpsSelection.Selection;
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
                    xpsSelection.ClearSelection();
                    reviewItemSelected = value;
                }
                else
                {
                    throw new Exception("Can only set ReviewItemSelected to false");
                }
            }
        }

        public ISpatialLocation GetReferenceLocationFromXml(XElement xmlLocation)
        {
            XElement xmlTopLeft = xmlLocation.Descendants("TopLeft").ElementAt(0);
            XElement xmlSize = xmlLocation.Descendants("Size").ElementAt(0);

            Rectangle rect = new Rectangle();

            rect.SetValue(Canvas.LeftProperty, double.Parse(xmlTopLeft.Attribute("Left").Value));
            rect.SetValue(Canvas.TopProperty, double.Parse(xmlTopLeft.Attribute("Top").Value));

            rect.Width = double.Parse(xmlSize.Attribute("Width").Value);
            rect.Height = double.Parse(xmlSize.Attribute("Height").Value);

            XpsLocation xpsLocation = new XpsLocation(rect);

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
                return new XpsLocation(selectedReviewItem);
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<FrameworkElement> CreateReferenceLocationHighlighting(ILocation referenceLocation, Brush highlightColor)
        {
            xpsSelection.ClearSelection();
            Rectangle rect = (referenceLocation as XpsLocation).Rectangle;
            rect.StrokeThickness = 2;
            rect.Fill = highlightColor;
            xpsSelection.XpsCanvas.Children.Add(rect);

            //rectangles will only ever contain one rectangle but we need an IEnumberable to be consist with the rest
            List<FrameworkElement> rectangles = new List<FrameworkElement>();
            rectangles.Add(rect);
            return (rectangles);
        }

        public void RemoveReferenceLocationHighlighting(IEnumerable<FrameworkElement> toBeRemoved)
        {
            //again this should contain one rectangle but just incase we changed to contain many this will still work
            foreach (FrameworkElement fe in toBeRemoved)
            {
                xpsSelection.XpsCanvas.Children.Remove(fe);
            }
        }

        public void SetReferenceLocationHighlightingToFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush highlightColor)
        {
            foreach (FrameworkElement fe in toBeHighlighted)
            {
                (fe as Rectangle).Fill = highlightColor;
                (fe as Rectangle).Stroke = new SolidColorBrush(Colors.Transparent);
            }
        }

        public void SetReferenceLocationHighlightingToNotFocused(IEnumerable<FrameworkElement> toBeHighlighted, Brush outlineColor)
        {
            foreach (FrameworkElement fe in toBeHighlighted)
            {
                (fe as Rectangle).Stroke = outlineColor;
                (fe as Rectangle).Fill = new SolidColorBrush(Colors.Transparent);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("ID", documentID.ToString());
        }

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